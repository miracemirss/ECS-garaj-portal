namespace ECS.Domain.Enums;

/// <summary>
/// Status of a history-tracked assignment (vehicle-trailer or driver-vehicle).
/// Exactly one Active record may exist per side at any moment.
/// </summary>
public enum AssignmentStatus
{
    Active = 1,
    Ended = 2
}
