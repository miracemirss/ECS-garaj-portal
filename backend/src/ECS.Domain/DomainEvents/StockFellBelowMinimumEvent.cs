using ECS.Domain.Common;

namespace ECS.Domain.DomainEvents;

/// <summary>
/// Raised when a part's stock reaches/falls below its minimum. Handled to create
/// a critical-stock alert.
/// </summary>
public sealed record StockFellBelowMinimumEvent(
    Guid PartId,
    decimal QuantityInStock,
    decimal MinimumStock) : DomainEvent;
