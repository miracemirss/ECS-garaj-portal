namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Transaction boundary owned by the Application layer. Critical multi-repository
/// operations are wrapped in a transaction so they commit or roll back atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction over a database transaction so the Application layer stays free of
/// EF Core types.
/// </summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
