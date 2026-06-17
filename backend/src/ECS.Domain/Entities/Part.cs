using ECS.Domain.Common;
using ECS.Domain.DomainEvents;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// An inventory part/SKU. QuantityInStock only ever changes through these methods,
/// which the Application pairs with a StockMovement record (mirrored by DB guards).
/// </summary>
public class Part : AggregateRoot
{
    private Part() { }

    private Part(string partNo, string name, string unit, decimal minimumStock, decimal unitCost)
    {
        PartNo = partNo;
        Name = name;
        Unit = unit;
        MinimumStock = minimumStock;
        UnitCost = unitCost;
        QuantityInStock = 0;
        IsActive = true;
    }

    public string PartNo { get; private set; } = null!;   // unique (SKU)
    public string Name { get; private set; } = null!;
    public string? Category { get; private set; }
    public string Unit { get; private set; } = "pcs";
    public decimal QuantityInStock { get; private set; }
    public decimal MinimumStock { get; private set; }
    public decimal UnitCost { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public bool IsActive { get; private set; }

    public Warehouse? Warehouse { get; private set; }
    public Supplier? Supplier { get; private set; }

    /// <summary>
    /// Kritik stok yalnızca anlamlı bir minimum tanımlandığında (MinimumStock &gt; 0)
    /// hesaplanır. MinimumStock = 0 ise parça "takip edilmiyor" kabul edilir ve stok 0
    /// olsa bile kritik sayılmaz.
    /// </summary>
    public bool IsBelowMinimum => MinimumStock > 0 && QuantityInStock <= MinimumStock;

    public static Part Create(string partNo, string name, string unit = "pcs", decimal minimumStock = 0, decimal unitCost = 0)
    {
        if (string.IsNullOrWhiteSpace(partNo))
        {
            throw new DomainException("Part number is required.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Part name is required.");
        }
        if (minimumStock < 0)
        {
            throw new DomainException("Minimum stock cannot be negative.");
        }
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }

        return new Part(partNo.Trim(), name.Trim(), string.IsNullOrWhiteSpace(unit) ? "pcs" : unit.Trim(), minimumStock, unitCost);
    }

    /// <summary>Goods receipt / return into stock.</summary>
    public void IncreaseStock(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Increase quantity must be positive.");
        }
        QuantityInStock += quantity;
    }

    /// <summary>Consumption out of stock. Throws if it would go negative; raises an event when at/under minimum.</summary>
    public void DecreaseStock(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Decrease quantity must be positive.");
        }
        if (QuantityInStock - quantity < 0)
        {
            throw new InsufficientStockException(Id, QuantityInStock, quantity);
        }

        QuantityInStock -= quantity;
        if (IsBelowMinimum)
        {
            RaiseDomainEvent(new StockFellBelowMinimumEvent(Id, QuantityInStock, MinimumStock));
        }
    }

    /// <summary>Signed stock-count correction.</summary>
    public void AdjustStock(decimal delta)
    {
        if (delta == 0)
        {
            throw new DomainException("Adjustment cannot be zero.");
        }
        if (QuantityInStock + delta < 0)
        {
            throw new InsufficientStockException(Id, QuantityInStock, -delta);
        }

        QuantityInStock += delta;
        if (IsBelowMinimum)
        {
            RaiseDomainEvent(new StockFellBelowMinimumEvent(Id, QuantityInStock, MinimumStock));
        }
    }

    public void SetMinimumStock(decimal minimumStock)
    {
        if (minimumStock < 0)
        {
            throw new DomainException("Minimum stock cannot be negative.");
        }
        MinimumStock = minimumStock;
    }

    public void SetUnitCost(decimal unitCost)
    {
        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }
        UnitCost = unitCost;
    }

    public void AssignWarehouse(Guid? warehouseId) => WarehouseId = warehouseId;
    public void AssignSupplier(Guid? supplierId) => SupplierId = supplierId;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
