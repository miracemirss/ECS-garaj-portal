using System.Linq.Expressions;
using ECS.Application.Common.Interfaces;
using ECS.Domain.Common;
using ECS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. It does NOT call
/// SaveChanges — persistence is committed via <see cref="UnitOfWork"/> so the
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

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
        => await Set.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await Set.Where(predicate).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<T> Items, int TotalCount)> PagedAsync(
        Expression<Func<T, bool>>? predicate, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = predicate is null ? Set : Set.Where(predicate);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(e => e.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null ? Set.CountAsync(cancellationToken) : Set.CountAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.AnyAsync(predicate, cancellationToken);

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
