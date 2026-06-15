using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// A part consumed by a work order. Linked 1:1 to the Out StockMovement that
/// recorded the consumption (so a line and its movement always exist together).
/// </summary>
public class WorkOrderPart : BaseEntity
{
    private WorkOrderPart() { }

    private WorkOrderPart(Guid workOrderId, Guid partId, decimal quantity, decimal unitCost, Guid? stockMovementId)
    {
        WorkOrderId = workOrderId;
        PartId = partId;
        Quantity = quantity;
        UnitCost = unitCost;
        StockMovementId = stockMovementId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid WorkOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal => Quantity * UnitCost;
    public Guid? StockMovementId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Part? Part { get; private set; }
    public StockMovement? StockMovement { get; private set; }

    public static WorkOrderPart Create(Guid workOrderId, Guid partId, decimal quantity, decimal unitCost, Guid? stockMovementId)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be positive.");
        }
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }
        return new WorkOrderPart(workOrderId, partId, quantity, unitCost, stockMovementId);
    }

    public void LinkStockMovement(Guid stockMovementId) => StockMovementId = stockMovementId;
}
