using ECS.Domain.Entities;

namespace ECS.Application.Common.Interfaces;

/// <summary>
/// User-specific persistence port. Needed because roles are a many-to-many
/// navigation that must be eager-loaded for token issuance (kept in Persistence
/// so the Application stays EF-free).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
}
