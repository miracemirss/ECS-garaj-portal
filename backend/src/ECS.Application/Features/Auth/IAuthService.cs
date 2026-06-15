using ECS.Application.Features.Auth.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<Result<AuthResultDto>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default);
    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result<UserSummaryDto>> GetCurrentAsync(CancellationToken ct = default);
}
