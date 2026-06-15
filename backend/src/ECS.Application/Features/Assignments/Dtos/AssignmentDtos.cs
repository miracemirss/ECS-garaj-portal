namespace ECS.Application.Features.Assignments.Dtos;

public sealed record AssignVehicleTrailerRequest(Guid VehicleId, Guid TrailerId, string? Note = null);

public sealed record AssignDriverVehicleRequest(Guid DriverId, Guid VehicleId, string? Note = null);

public sealed record VehicleTrailerAssignmentDto(
    Guid Id, Guid VehicleId, Guid TrailerId, string Status, DateTime StartedAt, DateTime? EndedAt);

public sealed record DriverVehicleAssignmentDto(
    Guid Id, Guid DriverId, Guid VehicleId, string Status, DateTime StartedAt, DateTime? EndedAt);
