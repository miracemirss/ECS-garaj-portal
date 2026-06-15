using System.Linq.Expressions;
using ECS.Domain.Common;

namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Generic persistence port. Implemented by Persistence/Repository&lt;T&gt; using
/// EF Core. The Application layer depends on this abstraction, never on DbContext,
/// so it stays free of EF types (predicates use System.Linq.Expressions).
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Filtered + sorted page (sort by Id when no orderBy given) plus the total count.</summary>
    Task<(IReadOnlyList<T> Items, int TotalCount)> PagedAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool descending,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Escape hatch for complex read queries built in the Persistence layer. Avoid in Application services.</summary>
    IQueryable<T> Query();

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
