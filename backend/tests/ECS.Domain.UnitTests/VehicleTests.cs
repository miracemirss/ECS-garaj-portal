using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class VehicleTests
{
    [Fact]
    public void Create_normalizes_plate_and_defaults_to_active()
    {
        var vehicle = Vehicle.Create(" 34 abc 123 ", "Mercedes", "Actros", 2022, vin: "wdb123");

        vehicle.PlateNo.Should().Be("34ABC123");
        vehicle.Brand.Should().Be("Mercedes");
        vehicle.Vin.Should().Be("WDB123");
        vehicle.Status.Should().Be(VehicleStatus.Active);
        vehicle.CurrentOdometerKm.Should().Be(0);
    }

    [Fact]
    public void Create_throws_when_brand_is_blank()
    {
        var act = () => Vehicle.Create("34ABC123", "  ");
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Create_throws_when_model_year_out_of_range(int year)
    {
        var act = () => Vehicle.Create("34ABC123", "Mercedes", modelYear: year);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateOdometer_rejects_moving_backwards()
    {
        var vehicle = Vehicle.Create("34ABC123", "Mercedes");
        vehicle.UpdateOdometer(1000);

        var act = () => vehicle.UpdateOdometer(900);

        act.Should().Throw<DomainException>();
        vehicle.CurrentOdometerKm.Should().Be(1000);
    }

    [Fact]
    public void RecordMaintenanceCompletion_rolls_odometer_and_schedules_next()
    {
        var vehicle = Vehicle.Create("34ABC123", "Mercedes");
        vehicle.SetMaintenancePlan(intervalKm: 30000, intervalDays: 180);
        var today = new DateOnly(2026, 6, 16);

        vehicle.RecordMaintenanceCompletion(120000, today);

        vehicle.CurrentOdometerKm.Should().Be(120000);
        vehicle.LastMaintenanceOdometerKm.Should().Be(120000);
        vehicle.NextMaintenanceKm.Should().Be(150000);
        vehicle.NextMaintenanceDate.Should().Be(today.AddDays(180));
    }
}
