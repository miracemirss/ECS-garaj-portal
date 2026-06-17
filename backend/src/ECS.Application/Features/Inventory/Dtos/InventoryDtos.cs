namespace ECS.Application.Features.Inventory.Dtos;

public sealed record PartDto(
    Guid Id,
    string PartNo,
    string Name,
    string? Category,
    string Unit,
    decimal QuantityInStock,
    decimal MinimumStock,
    decimal UnitCost,
    bool IsBelowMinimum,
    Guid? WarehouseId,
    Guid? SupplierId);

public sealed record StockMovementDto(
    Guid Id,
    string? MovementNo,
    Guid PartId,
    string MovementType,
    decimal Quantity,
    decimal BalanceAfter,
    decimal UnitCost,
    DateTime CreatedAt);

public sealed record CreatePartRequest(
    string PartNo,
    string Name,
    string Unit = "Adet",
    decimal InitialStock = 0,
    decimal MinimumStock = 0,
    decimal UnitCost = 0,
    Guid? WarehouseId = null,
    Guid? SupplierId = null);

public sealed record UpdatePartRequest(
    string Name,
    string? Category = null,
    decimal MinimumStock = 0,
    decimal UnitCost = 0);

public sealed record ReceiveStockRequest(Guid PartId, decimal Quantity, decimal UnitCost, Guid? WarehouseId = null, string? Note = null);

public sealed record IssueStockRequest(Guid PartId, decimal Quantity, string? Note = null);

public sealed record AdjustStockRequest(Guid PartId, decimal SignedQuantity, string? Note = null);
