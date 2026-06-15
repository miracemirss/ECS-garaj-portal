using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>An application user. Many-to-many with <see cref="Role"/> via the user_roles join.</summary>
public class User : AggregateRoot
{
    private readonly List<Role> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { }

    private User(string email, string fullName, string passwordHash)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
    }

    public string Email { get; private set; } = null!;     // unique
    public string FullName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? Phone { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsActive => Status == UserStatus.Active;

    public static User Create(string email, string fullName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.");
        }
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User(email.Trim().ToLowerInvariant(), fullName.Trim(), passwordHash);
    }

    public void AssignRole(Role role)
    {
        if (_roles.Any(r => r.Id == role.Id))
        {
            return;
        }
        _roles.Add(role);
    }

    public void RemoveRole(Role role) => _roles.RemoveAll(r => r.Id == role.Id);

    public void RecordLogin(DateTime utcNow) => LastLoginAt = utcNow;

    public void ChangeStatus(UserStatus status) => Status = status;

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }
        PasswordHash = passwordHash;
    }
}
