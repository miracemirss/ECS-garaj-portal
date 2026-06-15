using ECS.Application.Features.Alerts.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Alerts;

public interface IAlertService
{
    /// <summary>Adds a critical-stock alert (deduplicated) WITHOUT saving — composes inside a caller's transaction.</summary>
    Task EnsureCriticalStockAlertAsync(Guid partId, string partName, decimal quantityInStock, decimal minimumStock, CancellationToken ct = default);

    // Batch generators (invoked by the background jobs). Each manages its own save.
    Task<int> GenerateCriticalStockAlertsAsync(CancellationToken ct = default);
    Task<int> GenerateUpcomingMaintenanceAlertsAsync(CancellationToken ct = default);
    Task<int> GenerateDocumentExpiryAlertsAsync(CancellationToken ct = default);

    Task<Result> AcknowledgeAsync(Guid id, CancellationToken ct = default);
    Task<Result> ResolveAsync(Guid id, CancellationToken ct = default);
    Task<Result> DismissAsync(Guid id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AlertDto>>> GetOpenAsync(CancellationToken ct = default);
}
