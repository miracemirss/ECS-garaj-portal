using ECS.Domain.DomainEvents;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class MaintenanceWorkOrderTests
{
    private static MaintenanceWorkOrder OpenVehicleWorkOrder(int? before = 100000)
        => MaintenanceWorkOrder.CreateForVehicle(Guid.NewGuid(), "Periyodik bakım", WorkOrderType.Preventive, before);

    [Fact]
    public void CreateForVehicle_targets_vehicle_and_opens()
    {
        var wo = OpenVehicleWorkOrder();

        wo.TargetType.Should().Be(TargetType.Vehicle);
        wo.VehicleId.Should().NotBeNull();
        wo.TrailerId.Should().BeNull();
        wo.Status.Should().Be(WorkOrderStatus.Open);
    }

    [Fact]
    public void AddPart_accumulates_parts_cost_and_total()
    {
        var wo = OpenVehicleWorkOrder();
        wo.SetLaborCost(200);

        wo.AddPart(Guid.NewGuid(), quantity: 2, unitCost: 50); // 100
        wo.AddPart(Guid.NewGuid(), quantity: 1, unitCost: 30); // 30

        wo.PartsCost.Should().Be(130);
        wo.TotalCost.Should().Be(330); // labor 200 + parts 130
    }

    [Fact]
    public void Complete_from_open_sets_status_and_timestamp_and_raises_event()
    {
        var wo = OpenVehicleWorkOrder(before: 100000);
        var now = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        wo.Complete(odometerAfterKm: 101000, now);

        wo.Status.Should().Be(WorkOrderStatus.Completed);
        wo.CompletedAt.Should().Be(now);
        wo.OdometerAfterKm.Should().Be(101000);
        wo.DomainEvents.Should().ContainSingle(e => e is WorkOrderCompletedEvent);
    }

    [Fact]
    public void Complete_rejects_odometer_after_below_before()
    {
        var wo = OpenVehicleWorkOrder(before: 100000);

        var act = () => wo.Complete(odometerAfterKm: 99000, DateTime.UtcNow);

        act.Should().Throw<DomainException>();
        wo.Status.Should().Be(WorkOrderStatus.Open);
    }

    [Fact]
    public void Complete_twice_is_rejected()
    {
        var wo = OpenVehicleWorkOrder();
        wo.Complete(101000, DateTime.UtcNow);

        var act = () => wo.Complete(102000, DateTime.UtcNow);

        act.Should().Throw<InvalidWorkOrderStateException>();
    }

    [Fact]
    public void AddPart_is_rejected_after_completion()
    {
        var wo = OpenVehicleWorkOrder();
        wo.Complete(101000, DateTime.UtcNow);

        var act = () => wo.AddPart(Guid.NewGuid(), 1, 10);

        act.Should().Throw<InvalidWorkOrderStateException>();
    }

    [Fact]
    public void Start_moves_open_to_in_progress()
    {
        var wo = OpenVehicleWorkOrder();
        var now = DateTime.UtcNow;

        wo.Start(now);

        wo.Status.Should().Be(WorkOrderStatus.InProgress);
        wo.StartedAt.Should().Be(now);
    }
}
