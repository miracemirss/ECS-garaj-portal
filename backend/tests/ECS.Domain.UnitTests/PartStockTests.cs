using ECS.Domain.DomainEvents;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class PartStockTests
{
    private static Part NewPart(decimal initial, decimal minimum = 0)
    {
        var part = Part.Create("FLT-001", "Yağ Filtresi", "adet", minimumStock: minimum, unitCost: 50);
        if (initial > 0)
        {
            part.IncreaseStock(initial);
        }
        return part;
    }

    [Fact]
    public void New_part_starts_with_zero_stock()
        => Part.Create("P-1", "Part").QuantityInStock.Should().Be(0);

    [Fact]
    public void DecreaseStock_reduces_balance()
    {
        var part = NewPart(10);

        part.DecreaseStock(3);

        part.QuantityInStock.Should().Be(7);
    }

    [Fact]
    public void DecreaseStock_throws_when_insufficient_and_keeps_balance()
    {
        var part = NewPart(2);

        var act = () => part.DecreaseStock(5);

        act.Should().Throw<InsufficientStockException>();
        part.QuantityInStock.Should().Be(2, "a blocked consumption must not change the stock");
    }

    [Fact]
    public void DecreaseStock_raises_event_when_crossing_minimum()
    {
        var part = NewPart(10, minimum: 8);

        part.DecreaseStock(5); // 10 -> 5, below minimum 8

        part.IsBelowMinimum.Should().BeTrue();
        part.DomainEvents.Should().ContainSingle(e => e is StockFellBelowMinimumEvent);
    }

    [Fact]
    public void AdjustStock_cannot_drive_negative()
    {
        var part = NewPart(3);

        var act = () => part.AdjustStock(-5);

        act.Should().Throw<InsufficientStockException>();
        part.QuantityInStock.Should().Be(3);
    }

    [Fact]
    public void IncreaseStock_requires_positive_quantity()
    {
        var part = NewPart(0);
        var act = () => part.IncreaseStock(0);
        act.Should().Throw<DomainException>();
    }
}
