using System.Linq.Expressions;
using ECS.Domain.Common;

namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Generic persistence port. Implemented by Persistence/Repository&lt;T&gt; using
/// EF Core. The Application layer depends on this abstraction, never on DbContext.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Composable query for projections/filters. Materialize with async EF operators.</summary>
    IQueryable<T> Query();

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
