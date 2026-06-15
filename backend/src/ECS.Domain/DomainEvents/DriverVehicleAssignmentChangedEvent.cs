using ECS.Domain.Common;

namespace ECS.Domain.DomainEvents;

/// <summary>Raised when a driver-vehicle active assignment is created.</summary>
public sealed record DriverVehicleAssignmentChangedEvent(
    Guid AssignmentId,
    Guid DriverId,
    Guid VehicleId) : DomainEvent;
