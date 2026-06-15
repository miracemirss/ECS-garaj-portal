using ECS.Application.Common.Interfaces;
using ECS.Domain.Entities;
using ECS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Repositories;

/// <summary>User queries that eager-load the Roles many-to-many navigation.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
        => _context.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
}
