using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// History record of a driver-vehicle link. The ACTIVE record has EndedAt == null.
/// "One active vehicle per driver" is also enforced by a DB partial unique index.
/// </summary>
public class DriverVehicleAssignment : AggregateRoot
{
    private DriverVehicleAssignment() { }

    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public Guid? EndedBy { get; private set; }
    public string? Note { get; private set; }

    public Driver? Driver { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public bool IsActive => EndedAt is null;

    public static DriverVehicleAssignment Start(Guid driverId, Guid vehicleId, DateTime utcNow, string? note = null)
        => new()
        {
            DriverId = driverId,
            VehicleId = vehicleId,
            Status = AssignmentStatus.Active,
            StartedAt = utcNow,
            Note = note
        };

    public void End(DateTime utcNow, Guid? endedBy)
    {
        if (!IsActive)
        {
            throw new AssignmentConflictException("Assignment is already ended.");
        }
        if (utcNow < StartedAt)
        {
            throw new DomainException("End time cannot precede start time.");
        }

        EndedAt = utcNow;
        EndedBy = endedBy;
        Status = AssignmentStatus.Ended;
    }
}
