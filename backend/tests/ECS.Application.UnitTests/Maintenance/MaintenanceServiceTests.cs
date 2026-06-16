using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Alerts;
using ECS.Application.Features.Maintenance;
using ECS.Application.Features.Maintenance.Dtos;
using ECS.Application.Features.Reports;
using ECS.Application.Features.Reports.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Shared.Results;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ECS.Application.UnitTests.Maintenance;

public class MaintenanceServiceTests
{
    private readonly IRepository<MaintenanceWorkOrder> _workOrders = Substitute.For<IRepository<MaintenanceWorkOrder>>();
    private readonly IRepository<WorkOrderPart> _workOrderParts = Substitute.For<IRepository<WorkOrderPart>>();
    private readonly IRepository<MaintenanceTask> _tasks = Substitute.For<IRepository<MaintenanceTask>>();
    private readonly IRepository<Part> _parts = Substitute.For<IRepository<Part>>();
    private readonly IRepository<StockMovement> _movements = Substitute.For<IRepository<StockMovement>>();
    private readonly IRepository<Vehicle> _vehicles = Substitute.For<IRepository<Vehicle>>();
    private readonly IRepository<Trailer> _trailers = Substitute.For<IRepository<Trailer>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAuditLogService _audit = Substitute.For<IAuditLogService>();
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();
    private readonly INumberGenerator _numbers = Substitute.For<INumberGenerator>();
    private readonly IReportService _reports = Substitute.For<IReportService>();
    private readonly ILogger<MaintenanceService> _logger = Substitute.For<ILogger<MaintenanceService>>();

    // Real empty validators (validate as valid) — FluentValidation's ValidateAsync(T,...)
    // is an extension method that NSubstitute cannot intercept, so we avoid mocking it.
    private readonly IValidator<CreateWorkOrderRequest> _createValidator = new InlineValidator<CreateWorkOrderRequest>();
    private readonly IValidator<UpdateWorkOrderRequest> _updateValidator = new InlineValidator<UpdateWorkOrderRequest>();
    private readonly IValidator<AddWorkOrderPartRequest> _addPartValidator = new InlineValidator<AddWorkOrderPartRequest>();

    private MaintenanceService CreateSut()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc));
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IAppTransaction>());

        return new MaintenanceService(
            _workOrders, _workOrderParts, _tasks, _parts, _movements, _vehicles, _trailers,
            _uow, _clock, _currentUser, _audit, _alerts, _numbers, _reports, _logger,
            _createValidator, _updateValidator, _addPartValidator);
    }

    private static Part PartWithStock(decimal stock, decimal minimum = 0, decimal unitCost = 40)
    {
        var part = Part.Create("FLT-001", "Yağ Filtresi", "adet", minimum, unitCost);
        if (stock > 0)
        {
            part.IncreaseStock(stock);
        }
        return part;
    }

    [Fact]
    public async Task AddPart_blocks_and_keeps_stock_when_insufficient()
    {
        var sut = CreateSut();
        var wo = MaintenanceWorkOrder.CreateForVehicle(Guid.NewGuid(), "Bakım", WorkOrderType.Corrective);
        _workOrders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(wo);
        var part = PartWithStock(2);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await sut.AddPartAsync(wo.Id, new AddWorkOrderPartRequest(part.Id, 5), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
        part.QuantityInStock.Should().Be(2, "a blocked consumption must not change stock");
        await _movements.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPart_decrements_stock_and_records_out_movement()
    {
        var sut = CreateSut();
        _numbers.NextStockMovementNoAsync(Arg.Any<CancellationToken>()).Returns("SM-2026-000001");
        var wo = MaintenanceWorkOrder.CreateForVehicle(Guid.NewGuid(), "Bakım", WorkOrderType.Corrective);
        _workOrders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(wo);
        var part = PartWithStock(10, minimum: 1, unitCost: 40);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await sut.AddPartAsync(wo.Id, new AddWorkOrderPartRequest(part.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(3);
        result.Value.UnitCost.Should().Be(40);
        part.QuantityInStock.Should().Be(7);
        await _movements.Received(1).AddAsync(Arg.Is<StockMovement>(m => m.Quantity == -3m), Arg.Any<CancellationToken>());
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_marks_completed_rolls_vehicle_odometer_and_triggers_report()
    {
        var sut = CreateSut();
        var wo = MaintenanceWorkOrder.CreateForVehicle(Guid.NewGuid(), "Bakım", WorkOrderType.Preventive, odometerBeforeKm: 100000);
        _workOrders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(wo);

        var vehicle = Vehicle.Create("34ABC123", "Mercedes");
        vehicle.UpdateOdometer(100000);
        vehicle.SetMaintenancePlan(30000, 180);
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(vehicle);

        _reports.GenerateMaintenanceReportAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ReportFileDto(Guid.NewGuid(), "MaintenanceReport", wo.Id, "r.pdf", "application/pdf", 1L, DateTime.UtcNow)));

        var result = await sut.CompleteWorkOrderAsync(wo.Id, new CompleteWorkOrderRequest(101000), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        wo.Status.Should().Be(WorkOrderStatus.Completed);
        vehicle.CurrentOdometerKm.Should().Be(101000);
        await _reports.Received(1).GenerateMaintenanceReportAsync(wo.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_still_succeeds_when_report_generation_fails()
    {
        var sut = CreateSut();
        var wo = MaintenanceWorkOrder.CreateForVehicle(Guid.NewGuid(), "Bakım", WorkOrderType.Preventive, odometerBeforeKm: 100000);
        _workOrders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(wo);
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Vehicle.Create("34ABC123", "Mercedes"));
        _reports.GenerateMaintenanceReportAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ReportFileDto>(Error.Failure("pdf boom")));

        var result = await sut.CompleteWorkOrderAsync(wo.Id, new CompleteWorkOrderRequest(101000), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("completion must never depend on PDF success");
        wo.Status.Should().Be(WorkOrderStatus.Completed);
        await _audit.Received().LogAsync("MaintenanceReportGenerationFailed",
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
