namespace ECS.Application.Features.Alerts.Dtos;

public sealed record AlertDto(
    Guid Id,
    string AlertType,
    string Priority,
    string Status,
    string Title,
    string? Message,
    Guid? PartId,
    Guid? VehicleId,
    Guid? TrailerId,
    DateTime CreatedAt);
