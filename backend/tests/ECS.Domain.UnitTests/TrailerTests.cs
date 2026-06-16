using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class TrailerTests
{
    [Fact]
    public void Create_normalizes_plate_and_keeps_type()
    {
        var trailer = Trailer.Create(" 34 td 4545 ", trailerType: "Frigo", capacityKg: 24000, vin: "nvr987");

        trailer.PlateNo.Should().Be("34TD4545");
        trailer.TrailerType.Should().Be("Frigo");
        trailer.CapacityKg.Should().Be(24000);
        trailer.Vin.Should().Be("NVR987");
        trailer.Status.Should().Be(TrailerStatus.Active);
    }

    [Fact]
    public void Create_throws_on_negative_capacity()
    {
        var act = () => Trailer.Create("34TD4545", capacityKg: -1);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_throws_when_tire_condition_out_of_range(int tire)
    {
        var act = () => Trailer.Create("34TD4545", tireConditionPercent: tire);
        act.Should().Throw<DomainException>();
    }
}
