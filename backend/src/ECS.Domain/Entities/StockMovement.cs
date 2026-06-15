using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// Append-only stock ledger entry. Quantity is SIGNED: In/Return &gt; 0, Out &lt; 0,
/// Adjustment &lt;&gt; 0. BalanceAfter is the resulting part stock, stamped by the
/// Application/DB when the movement is applied.
/// </summary>
public class StockMovement : BaseEntity
{
    private StockMovement() { }

    private StockMovement(Guid partId, StockMovementType type, decimal quantity, decimal unitCost)
    {
        PartId = partId;
        MovementType = type;
        Quantity = quantity;
        UnitCost = unitCost;
        CreatedAt = DateTime.UtcNow;
    }

    public string? MovementNo { get; private set; }    // assigned on persistence (DB trigger / service)
    public Guid PartId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }      // signed
    public decimal UnitCost { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Part? Part { get; private set; }

    public static StockMovement In(Guid partId, decimal quantity, decimal unitCost, Guid? warehouseId = null, Guid? supplierId = null, string? note = null)
        => CreatePositive(partId, StockMovementType.In, quantity, unitCost, warehouseId, supplierId, null, note);

    public static StockMovement Return(Guid partId, decimal quantity, decimal unitCost, Guid? warehouseId = null, string? note = null)
        => CreatePositive(partId, StockMovementType.Return, quantity, unitCost, warehouseId, null, null, note);

    public static StockMovement Out(Guid partId, decimal quantity, decimal unitCost, Guid? warehouseId = null, Guid? workOrderId = null, string? note = null)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Out quantity magnitude must be positive.");
        }
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }
        return new StockMovement(partId, StockMovementType.Out, -quantity, unitCost)
        {
            WarehouseId = warehouseId,
            WorkOrderId = workOrderId,
            Note = note
        };
    }

    public static StockMovement Adjustment(Guid partId, decimal signedQuantity, decimal unitCost, Guid? warehouseId = null, string? note = null)
    {
        if (signedQuantity == 0)
        {
            throw new DomainException("Adjustment quantity cannot be zero.");
        }
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }
        return new StockMovement(partId, StockMovementType.Adjustment, signedQuantity, unitCost)
        {
            WarehouseId = warehouseId,
            Note = note
        };
    }

    private static StockMovement CreatePositive(
        Guid partId, StockMovementType type, decimal quantity, decimal unitCost,
        Guid? warehouseId, Guid? supplierId, Guid? workOrderId, string? note)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be positive.");
        }
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }
        return new StockMovement(partId, type, quantity, unitCost)
        {
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            WorkOrderId = workOrderId,
            Note = note
        };
    }

    public void StampBalance(decimal balanceAfter) => BalanceAfter = balanceAfter;

    public void AssignNumber(string movementNo)
    {
        if (string.IsNullOrWhiteSpace(movementNo))
        {
            throw new DomainException("Movement number is required.");
        }
        MovementNo = movementNo;
    }
}
