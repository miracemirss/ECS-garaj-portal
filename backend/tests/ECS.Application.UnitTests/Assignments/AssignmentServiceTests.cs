using System.Linq.Expressions;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Assignments;
using ECS.Application.Features.Assignments.Dtos;
using ECS.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace ECS.Application.UnitTests.Assignments;

public class AssignmentServiceTests
{
    private readonly IRepository<VehicleTrailerAssignment> _vehicleTrailer = Substitute.For<IRepository<VehicleTrailerAssignment>>();
    private readonly IRepository<DriverVehicleAssignment> _driverVehicle = Substitute.For<IRepository<DriverVehicleAssignment>>();
    private readonly IRepository<Vehicle> _vehicles = Substitute.For<IRepository<Vehicle>>();
    private readonly IRepository<Trailer> _trailers = Substitute.For<IRepository<Trailer>>();
    private readonly IRepository<Driver> _drivers = Substitute.For<IRepository<Driver>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAuditLogService _audit = Substitute.For<IAuditLogService>();
    // Real empty validators (validate as valid); see note in MaintenanceServiceTests.
    private readonly IValidator<AssignVehicleTrailerRequest> _vtValidator = new InlineValidator<AssignVehicleTrailerRequest>();
    private readonly IValidator<AssignDriverVehicleRequest> _dvValidator = new InlineValidator<AssignDriverVehicleRequest>();

    private readonly DateTime _now = new(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);

    private AssignmentService CreateSut()
    {
        _clock.UtcNow.Returns(_now);
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IAppTransaction>());

        return new AssignmentService(
            _vehicleTrailer, _driverVehicle, _vehicles, _trailers, _drivers,
            _uow, _clock, _currentUser, _audit, _vtValidator, _dvValidator);
    }

    [Fact]
    public async Task Assign_vehicle_trailer_closes_existing_active_link_before_opening_new()
    {
        var sut = CreateSut();
        var vehicleId = Guid.NewGuid();
        var trailerId = Guid.NewGuid();
        _vehicles.AnyAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);
        _trailers.AnyAsync(Arg.Any<Expression<Func<Trailer, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var existing = VehicleTrailerAssignment.Start(vehicleId, Guid.NewGuid(), _now.AddDays(-2));
        _vehicleTrailer.ListAsync(Arg.Any<Expression<Func<VehicleTrailerAssignment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<VehicleTrailerAssignment> { existing });

        var result = await sut.AssignVehicleTrailerAsync(new AssignVehicleTrailerRequest(vehicleId, trailerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeFalse("the prior active link must be ended before a new one opens");
        await _vehicleTrailer.Received(1).AddAsync(
            Arg.Is<VehicleTrailerAssignment>(a => a.VehicleId == vehicleId && a.TrailerId == trailerId && a.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_vehicle_trailer_returns_not_found_when_vehicle_missing()
    {
        var sut = CreateSut();
        _vehicles.AnyAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await sut.AssignVehicleTrailerAsync(new AssignVehicleTrailerRequest(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
        await _vehicleTrailer.DidNotReceive().AddAsync(Arg.Any<VehicleTrailerAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_driver_vehicle_closes_existing_active_link_before_opening_new()
    {
        var sut = CreateSut();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        _drivers.AnyAsync(Arg.Any<Expression<Func<Driver, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);
        _vehicles.AnyAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var existing = DriverVehicleAssignment.Start(driverId, Guid.NewGuid(), _now.AddDays(-3));
        _driverVehicle.ListAsync(Arg.Any<Expression<Func<DriverVehicleAssignment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DriverVehicleAssignment> { existing });

        var result = await sut.AssignDriverVehicleAsync(new AssignDriverVehicleRequest(driverId, vehicleId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeFalse();
        await _driverVehicle.Received(1).AddAsync(
            Arg.Is<DriverVehicleAssignment>(a => a.DriverId == driverId && a.VehicleId == vehicleId && a.IsActive),
            Arg.Any<CancellationToken>());
    }
}
