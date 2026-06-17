using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Alerts.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Shared.Results;

namespace ECS.Application.Features.Alerts;

public sealed class AlertService : IAlertService
{
    private readonly IRepository<Alert> _alerts;
    private readonly IRepository<Part> _parts;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Document> _documents;
    private readonly IRepository<CompanySetting> _settings;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;

    public AlertService(
        IRepository<Alert> alerts,
        IRepository<Part> parts,
        IRepository<Vehicle> vehicles,
        IRepository<Document> documents,
        IRepository<CompanySetting> settings,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit)
    {
        _alerts = alerts;
        _parts = parts;
        _vehicles = vehicles;
        _documents = documents;
        _settings = settings;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task EnsureCriticalStockAlertAsync(Guid partId, string partName, decimal quantityInStock, decimal minimumStock, CancellationToken ct = default)
    {
        var key = $"stock:{partId}";
        if (await _alerts.AnyAsync(a => a.DedupKey == key && a.Status == AlertStatus.Open, ct))
        {
            return;
        }

        await _alerts.AddAsync(
            Alert.ForCriticalStock(partId, $"Kritik stok: {partName}",
                $"{partName}: stok {quantityInStock}, minimum {minimumStock}"),
            ct);
        // No SaveChanges: the caller's transaction commits this alert atomically.
    }

    public async Task<int> GenerateCriticalStockAlertsAsync(CancellationToken ct = default)
    {
        var parts = await _parts.ListAsync(p => p.IsActive && !p.IsDeleted && p.MinimumStock > 0 && p.QuantityInStock <= p.MinimumStock, ct);
        var created = 0;

        foreach (var part in parts)
        {
            var key = $"stock:{part.Id}";
            if (await _alerts.AnyAsync(a => a.DedupKey == key && a.Status == AlertStatus.Open, ct))
            {
                continue;
            }
            await _alerts.AddAsync(
                Alert.ForCriticalStock(part.Id, $"Kritik stok: {part.Name}",
                    $"{part.Name} ({part.PartNo}): stok {part.QuantityInStock}, minimum {part.MinimumStock}"),
                ct);
            created++;
        }

        if (created > 0)
        {
            await _uow.SaveChangesAsync(ct);
            await _audit.LogAsync("CriticalStockAlertsGenerated", nameof(Alert), null, new { created }, ct);
        }
        return created;
    }

    public async Task<int> GenerateUpcomingMaintenanceAlertsAsync(CancellationToken ct = default)
    {
        var settings = await _settings.FirstOrDefaultAsync(_ => true, ct);
        var kmThreshold = settings?.MaintenanceDueKmThreshold ?? 1000;
        var daysThreshold = settings?.MaintenanceDueDaysThreshold ?? 14;
        var dueDate = DateOnly.FromDateTime(_clock.UtcNow).AddDays(daysThreshold);

        var vehicles = await _vehicles.ListAsync(v =>
            !v.IsDeleted && v.Status != VehicleStatus.Retired &&
            ((v.NextMaintenanceDate != null && v.NextMaintenanceDate <= dueDate) ||
             (v.NextMaintenanceKm != null && v.NextMaintenanceKm - v.CurrentOdometerKm <= kmThreshold)), ct);

        var created = 0;
        foreach (var v in vehicles)
        {
            var key = $"maint:{v.Id}";
            if (await _alerts.AnyAsync(a => a.DedupKey == key && a.Status == AlertStatus.Open, ct))
            {
                continue;
            }
            await _alerts.AddAsync(
                Alert.ForMaintenanceDue(v.Id, $"Yaklaşan bakım: {v.PlateNo}",
                    $"{v.PlateNo} — sonraki bakım km {v.NextMaintenanceKm}, tarih {v.NextMaintenanceDate}"),
                ct);
            created++;
        }

        if (created > 0)
        {
            await _uow.SaveChangesAsync(ct);
            await _audit.LogAsync("MaintenanceDueAlertsGenerated", nameof(Alert), null, new { created }, ct);
        }
        return created;
    }

    public async Task<int> GenerateDocumentExpiryAlertsAsync(CancellationToken ct = default)
    {
        var settings = await _settings.FirstOrDefaultAsync(_ => true, ct);
        var daysThreshold = settings?.DocumentExpiryDaysThreshold ?? 30;
        var dueDate = DateOnly.FromDateTime(_clock.UtcNow).AddDays(daysThreshold);

        var documents = await _documents.ListAsync(d =>
            !d.IsDeleted && d.ExpiryDate != null && d.ExpiryDate <= dueDate, ct);

        var created = 0;
        foreach (var doc in documents)
        {
            var key = $"doc:{doc.Id}";
            if (await _alerts.AnyAsync(a => a.DedupKey == key && a.Status == AlertStatus.Open, ct))
            {
                continue;
            }
            await _alerts.AddAsync(
                Alert.Create(AlertType.DocumentExpiry, AlertPriority.Warning,
                    $"Belge bitişi: {doc.Title}", $"{doc.Title} — bitiş {doc.ExpiryDate}", key),
                ct);
            created++;
        }

        if (created > 0)
        {
            await _uow.SaveChangesAsync(ct);
            await _audit.LogAsync("DocumentExpiryAlertsGenerated", nameof(Alert), null, new { created }, ct);
        }
        return created;
    }

    public async Task<Result> AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await _alerts.GetByIdAsync(id, ct);
        if (alert is null)
        {
            return Result.Failure(Error.NotFound($"Alert {id} was not found."));
        }
        alert.Acknowledge(_currentUser.UserId, _clock.UtcNow);
        _alerts.Update(alert);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ResolveAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await _alerts.GetByIdAsync(id, ct);
        if (alert is null)
        {
            return Result.Failure(Error.NotFound($"Alert {id} was not found."));
        }
        alert.Resolve(_currentUser.UserId, _clock.UtcNow);
        _alerts.Update(alert);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("AlertResolved", nameof(Alert), alert.Id.ToString(), null, ct);
        return Result.Success();
    }

    public async Task<Result> DismissAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await _alerts.GetByIdAsync(id, ct);
        if (alert is null)
        {
            return Result.Failure(Error.NotFound($"Alert {id} was not found."));
        }
        alert.Dismiss();
        _alerts.Update(alert);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AlertDto>>> GetOpenAsync(CancellationToken ct = default)
    {
        var alerts = await _alerts.ListAsync(a => a.Status == AlertStatus.Open, ct);
        IReadOnlyList<AlertDto> dtos = alerts
            .OrderByDescending(a => a.CreatedAt)
            .Select(MapToDto)
            .ToList();
        return Result.Success(dtos);
    }

    private static AlertDto MapToDto(Alert a) => new(
        a.Id, a.AlertType.ToString(), a.Priority.ToString(), a.Status.ToString(),
        a.Title, a.Message, a.PartId, a.VehicleId, a.TrailerId, a.CreatedAt);
}
