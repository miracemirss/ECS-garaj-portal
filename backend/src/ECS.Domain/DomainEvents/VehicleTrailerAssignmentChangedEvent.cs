using ECS.Domain.Common;

namespace ECS.Domain.DomainEvents;

/// <summary>Raised when a vehicle-trailer active assignment is created.</summary>
public sealed record VehicleTrailerAssignmentChangedEvent(
    Guid AssignmentId,
    Guid VehicleId,
    Guid TrailerId) : DomainEvent;
