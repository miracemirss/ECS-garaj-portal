namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Produces human-friendly sequential numbers (e.g. WO-2026-000123, SM-2026-000123).
/// Implemented in Persistence over DB sequences so numbers stay gap-tolerant and unique.
/// </summary>
public interface INumberGenerator
{
    Task<string> NextWorkOrderNoAsync(CancellationToken cancellationToken = default);
    Task<string> NextStockMovementNoAsync(CancellationToken cancellationToken = default);
}
