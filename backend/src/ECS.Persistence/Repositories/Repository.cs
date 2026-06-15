using System.Linq.Expressions;
using ECS.Application.Common.Interfaces;
using ECS.Domain.Common;
using ECS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. Note that it does NOT
/// call SaveChanges — persistence is committed via <see cref="UnitOfWork"/> so the
/// Application layer controls the transaction boundary.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> Set;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
        => await Set.ToListAsync(cancellationToken);

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.AnyAsync(predicate, cancellationToken);
}
