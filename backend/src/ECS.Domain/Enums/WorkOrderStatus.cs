namespace ECS.Domain.Enums;

/// <summary>
/// Lifecycle of a maintenance work order.
/// </summary>
public enum WorkOrderStatus
{
    Draft = 0,
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
