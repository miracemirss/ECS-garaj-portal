using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A hashed refresh token issued to a user. Raw tokens are never stored.</summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;   // unique
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public User? User { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime utcNow, DateTime expiresAt, string? ip = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.");
        }
        if (expiresAt <= utcNow)
        {
            throw new DomainException("Refresh token expiry must be in the future.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = utcNow,
            ExpiresAt = expiresAt,
            CreatedByIp = ip
        };
    }

    public void Revoke(DateTime utcNow, string? replacedByTokenHash = null, string? reason = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }
        RevokedAt = utcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        ReasonRevoked = reason;
    }
}
