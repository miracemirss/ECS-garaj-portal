namespace ECS.Application.Features.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record UserSummaryDto(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);

public sealed record AuthResultDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    UserSummaryDto User);
