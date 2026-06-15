using ECS.Application.Common.Interfaces;

namespace ECS.Infrastructure.Identity;

/// <summary>
/// Issues JWT access tokens and refresh tokens. Signing-key handling and claim
/// construction are implemented in the auth prompt.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    public AccessToken CreateAccessToken(Guid userId, string userName, IEnumerable<string> roles)
        => throw new NotImplementedException("JWT issuance is implemented in the auth prompt.");

    public string CreateRefreshToken()
        => throw new NotImplementedException("Refresh token generation is implemented in the auth prompt.");
}
