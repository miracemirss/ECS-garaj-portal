using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// A system-generated alert (critical stock, upcoming maintenance, document
/// expiry). DedupKey keeps at most one OPEN alert per subject.
/// </summary>
public class Alert : BaseEntity
{
    private Alert() { }

    private Alert(AlertType type, AlertPriority priority, string title)
    {
        AlertType = type;
        Priority = priority;
        Title = title;
        Status = AlertStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public AlertType AlertType { get; private set; }
    public AlertPriority Priority { get; private set; }
    public AlertStatus Status { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Message { get; private set; }
    public Guid? PartId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? TrailerId { get; private set; }
    public string? DedupKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }

    public static Alert ForCriticalStock(Guid partId, string title, string? message)
        => new(AlertType.CriticalStock, AlertPriority.Critical, title)
        {
            PartId = partId,
            Message = message,
            DedupKey = $"stock:{partId}"
        };

    public static Alert ForMaintenanceDue(Guid vehicleId, string title, string? message, AlertPriority priority = AlertPriority.Warning)
        => new(AlertType.MaintenanceDue, priority, title)
        {
            VehicleId = vehicleId,
            Message = message,
            DedupKey = $"maint:{vehicleId}"
        };

    public static Alert Create(AlertType type, AlertPriority priority, string title, string? message = null, string? dedupKey = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Alert title is required.");
        }
        return new Alert(type, priority, title) { Message = message, DedupKey = dedupKey };
    }

    public void Acknowledge(Guid? userId, DateTime utcNow)
    {
        if (Status != AlertStatus.Open)
        {
            return;
        }
        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = utcNow;
        AcknowledgedBy = userId;
    }

    public void Resolve(Guid? userId, DateTime utcNow)
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = utcNow;
        ResolvedBy = userId;
    }

    public void Dismiss() => Status = AlertStatus.Dismissed;
}
