using ECS.Domain.Common;
using ECS.Domain.Enums;

namespace ECS.Domain.DomainEvents;

/// <summary>
/// Raised when a work order is completed. Handled in the Application/Infrastructure
/// layers to generate the maintenance PDF report and write audit context.
/// </summary>
public sealed record WorkOrderCompletedEvent(
    Guid WorkOrderId,
    TargetType TargetType,
    Guid? VehicleId,
    Guid? TrailerId,
    int? OdometerAfterKm) : DomainEvent;
