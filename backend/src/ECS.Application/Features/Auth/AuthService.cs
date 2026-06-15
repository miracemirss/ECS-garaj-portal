using System.Security.Cryptography;
using System.Text;
using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Auth.Dtos;
using ECS.Domain.Entities;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRepository<RefreshToken> _refreshTokens;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthService(
        IUserRepository users,
        IRepository<RefreshToken> refreshTokens,
        IJwtTokenService jwt,
        IPasswordHasher hasher,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _hasher = hasher;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    public async Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<AuthResultDto>(Error.Validation(validation.ToMessage()));
        }

        var user = await _users.GetByEmailWithRolesAsync(request.Email.Trim().ToLowerInvariant(), ct);
        // Same response whether the user is missing or the password is wrong (no enumeration).
        if (user is null || !user.IsActive || !_hasher.Verify(user.PasswordHash, request.Password))
        {
            return Result.Failure<AuthResultDto>(Error.Unauthorized("Invalid email or password."));
        }

        var now = _clock.UtcNow;
        var roles = user.Roles.Select(r => r.Name).ToList();
        var access = _jwt.CreateAccessToken(user.Id, user.FullName, roles);
        var refresh = _jwt.CreateRefreshToken();

        await _refreshTokens.AddAsync(RefreshToken.Issue(user.Id, Sha256(refresh.Token), now, refresh.ExpiresAtUtc, ipAddress), ct);
        user.RecordLogin(now);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("UserLoggedIn", nameof(User), user.Id.ToString(), null, ct);

        return Result.Success(new AuthResultDto(access.Token, access.ExpiresAtUtc, refresh.Token,
            new UserSummaryDto(user.Id, user.Email, user.FullName, roles)));
    }

    public async Task<Result<AuthResultDto>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<AuthResultDto>(Error.Validation(validation.ToMessage()));
        }

        var now = _clock.UtcNow;
        var incomingHash = Sha256(request.RefreshToken);
        var stored = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == incomingHash, ct);
        if (stored is null || !stored.IsActive(now))
        {
            return Result.Failure<AuthResultDto>(Error.Unauthorized("Invalid or expired refresh token."));
        }

        var user = await _users.GetByIdWithRolesAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResultDto>(Error.Unauthorized("User is not active."));
        }

        var roles = user.Roles.Select(r => r.Name).ToList();
        var access = _jwt.CreateAccessToken(user.Id, user.FullName, roles);
        var refresh = _jwt.CreateRefreshToken();
        var newHash = Sha256(refresh.Token);

        // Rotate: revoke the used token and issue a new one.
        stored.Revoke(now, newHash, "rotated");
        _refreshTokens.Update(stored);
        await _refreshTokens.AddAsync(RefreshToken.Issue(user.Id, newHash, now, refresh.ExpiresAtUtc, ipAddress), ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AuthResultDto(access.Token, access.ExpiresAtUtc, refresh.Token,
            new UserSummaryDto(user.Id, user.Email, user.FullName, roles)));
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = Sha256(request.RefreshToken);
        var stored = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is not null)
        {
            stored.Revoke(_clock.UtcNow, reason: "logout");
            _refreshTokens.Update(stored);
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<UserSummaryDto>> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<UserSummaryDto>(Error.Unauthorized("Not authenticated."));
        }

        var user = await _users.GetByIdWithRolesAsync(userId, ct);
        return user is null
            ? Result.Failure<UserSummaryDto>(Error.NotFound("User was not found."))
            : Result.Success(new UserSummaryDto(user.Id, user.Email, user.FullName, user.Roles.Select(r => r.Name).ToList()));
    }

    private static string Sha256(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
