namespace ECS.Domain.Enums;

/// <summary>
/// Direction/kind of a stock movement. Stock quantity changes ONLY through a
/// StockMovement record — never by direct assignment.
/// </summary>
public enum StockMovementType
{
    In = 1,          // purchase / goods receipt
    Out = 2,         // consumed by a work order
    Adjustment = 3,  // manual correction (stock count)
    Return = 4       // returned to stock
}
