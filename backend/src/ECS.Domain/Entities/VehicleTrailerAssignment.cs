using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// History record of a vehicle-trailer link. The ACTIVE record has EndedAt == null.
/// "One active link per side" is also enforced by a DB partial unique index.
/// </summary>
public class VehicleTrailerAssignment : AggregateRoot
{
    private VehicleTrailerAssignment() { }

    public Guid VehicleId { get; private set; }
    public Guid TrailerId { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public Guid? EndedBy { get; private set; }
    public string? Note { get; private set; }

    public Vehicle? Vehicle { get; private set; }
    public Trailer? Trailer { get; private set; }

    public bool IsActive => EndedAt is null;

    public static VehicleTrailerAssignment Start(Guid vehicleId, Guid trailerId, DateTime utcNow, string? note = null)
        => new()
        {
            VehicleId = vehicleId,
            TrailerId = trailerId,
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
