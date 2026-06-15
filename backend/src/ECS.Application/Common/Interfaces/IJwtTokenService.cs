namespace ECS.Application.Common.Interfaces;

/// <summary>Issues access and refresh tokens. Implemented in Infrastructure.</summary>
public interface IJwtTokenService
{
    AccessToken CreateAccessToken(Guid userId, string userName, IEnumerable<string> roles);
    RefreshTokenResult CreateRefreshToken();
}

/// <summary>A signed access token and its expiry.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>A freshly generated refresh token (raw value) and its expiry.</summary>
public sealed record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);
