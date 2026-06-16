using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class AssignmentTests
{
    [Fact]
    public void VehicleTrailer_starts_active()
    {
        var a = VehicleTrailerAssignment.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        a.IsActive.Should().BeTrue();
        a.EndedAt.Should().BeNull();
        a.Status.Should().Be(AssignmentStatus.Active);
    }

    [Fact]
    public void VehicleTrailer_end_marks_inactive()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var a = VehicleTrailerAssignment.Start(Guid.NewGuid(), Guid.NewGuid(), start);

        a.End(start.AddHours(5), endedBy: Guid.NewGuid());

        a.IsActive.Should().BeFalse();
        a.Status.Should().Be(AssignmentStatus.Ended);
        a.EndedAt.Should().Be(start.AddHours(5));
    }

    [Fact]
    public void VehicleTrailer_end_twice_throws_conflict()
    {
        var a = VehicleTrailerAssignment.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        a.End(DateTime.UtcNow, null);

        var act = () => a.End(DateTime.UtcNow, null);

        act.Should().Throw<AssignmentConflictException>();
    }

    [Fact]
    public void DriverVehicle_lifecycle()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var a = DriverVehicleAssignment.Start(Guid.NewGuid(), Guid.NewGuid(), start);

        a.IsActive.Should().BeTrue();
        a.End(start.AddDays(1), null);
        a.IsActive.Should().BeFalse();
    }

    [Fact]
    public void End_before_start_is_rejected()
    {
        var start = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc);
        var a = DriverVehicleAssignment.Start(Guid.NewGuid(), Guid.NewGuid(), start);

        var act = () => a.End(start.AddDays(-1), null);

        act.Should().Throw<DomainException>();
    }
}
