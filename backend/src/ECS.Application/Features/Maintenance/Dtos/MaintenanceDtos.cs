namespace ECS.Application.Features.Maintenance.Dtos;

public sealed record CreateWorkOrderRequest(
    string TargetType,            // "Vehicle" | "Trailer"
    Guid TargetId,
    string Title,
    string MaintenanceType = "Corrective",
    int? OdometerBeforeKm = null,
    decimal LaborCost = 0,
    DateOnly? ScheduledDate = null,
    string? Description = null);

public sealed record UpdateWorkOrderRequest(
    string Title,
    string? Description = null,
    decimal LaborCost = 0,
    DateOnly? ScheduledDate = null);

public sealed record AddWorkOrderPartRequest(Guid PartId, decimal Quantity, decimal? UnitCost = null);

public sealed record AddTaskRequest(string Description, decimal? LaborHours = null);

public sealed record CompleteWorkOrderRequest(int? OdometerAfterKm = null);

public sealed record WorkOrderPartDto(Guid Id, Guid PartId, decimal Quantity, decimal UnitCost, decimal LineTotal);

public sealed record WorkOrderDto(
    Guid Id,
    string? WorkOrderNo,
    string TargetType,
    Guid? VehicleId,
    Guid? TrailerId,
    string Status,
    string MaintenanceType,
    string Title,
    string? Description,
    int? OdometerBeforeKm,
    int? OdometerAfterKm,
    decimal LaborCost,
    decimal PartsCost,
    decimal TotalCost,
    DateOnly? ScheduledDate,
    DateTime? CompletedAt,
    IReadOnlyList<WorkOrderPartDto> Parts);
