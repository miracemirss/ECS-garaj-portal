using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Alerts;
using ECS.Application.Features.Maintenance.Dtos;
using ECS.Application.Features.Reports;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Maintenance;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IRepository<MaintenanceWorkOrder> _workOrders;
    private readonly IRepository<WorkOrderPart> _workOrderParts;
    private readonly IRepository<MaintenanceTask> _tasks;
    private readonly IRepository<Part> _parts;
    private readonly IRepository<StockMovement> _movements;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Trailer> _trailers;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IAlertService _alerts;
    private readonly INumberGenerator _numbers;
    private readonly IReportService _reportService;
    private readonly IValidator<CreateWorkOrderRequest> _createValidator;
    private readonly IValidator<UpdateWorkOrderRequest> _updateValidator;
    private readonly IValidator<AddWorkOrderPartRequest> _addPartValidator;

    public MaintenanceService(
        IRepository<MaintenanceWorkOrder> workOrders,
        IRepository<WorkOrderPart> workOrderParts,
        IRepository<MaintenanceTask> tasks,
        IRepository<Part> parts,
        IRepository<StockMovement> movements,
        IRepository<Vehicle> vehicles,
        IRepository<Trailer> trailers,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IAlertService alerts,
        INumberGenerator numbers,
        IReportService reportService,
        IValidator<CreateWorkOrderRequest> createValidator,
        IValidator<UpdateWorkOrderRequest> updateValidator,
        IValidator<AddWorkOrderPartRequest> addPartValidator)
    {
        _workOrders = workOrders;
        _workOrderParts = workOrderParts;
        _tasks = tasks;
        _parts = parts;
        _movements = movements;
        _vehicles = vehicles;
        _trailers = trailers;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _alerts = alerts;
        _numbers = numbers;
        _reportService = reportService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addPartValidator = addPartValidator;
    }

    public async Task<Result<WorkOrderDto>> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<WorkOrderDto>(Error.Validation(validation.ToMessage()));
        }

        var targetType = Enum.Parse<TargetType>(request.TargetType);
        var maintenanceType = Enum.Parse<WorkOrderType>(request.MaintenanceType);

        MaintenanceWorkOrder workOrder;
        try
        {
            if (targetType == TargetType.Vehicle)
            {
                if (!await _vehicles.AnyAsync(v => v.Id == request.TargetId && !v.IsDeleted, ct))
                {
                    return Result.Failure<WorkOrderDto>(Error.NotFound($"Vehicle {request.TargetId} was not found."));
                }
                workOrder = MaintenanceWorkOrder.CreateForVehicle(request.TargetId, request.Title, maintenanceType, request.OdometerBeforeKm);
            }
            else
            {
                if (!await _trailers.AnyAsync(t => t.Id == request.TargetId && !t.IsDeleted, ct))
                {
                    return Result.Failure<WorkOrderDto>(Error.NotFound($"Trailer {request.TargetId} was not found."));
                }
                workOrder = MaintenanceWorkOrder.CreateForTrailer(request.TargetId, request.Title, maintenanceType);
            }

            workOrder.SetLaborCost(request.LaborCost);
            workOrder.SetDescription(request.Description);
            if (request.ScheduledDate is { } scheduled)
            {
                workOrder.Schedule(scheduled);
            }
            workOrder.AssignNumber(await _numbers.NextWorkOrderNoAsync(ct));
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkOrderDto>(Error.Validation(ex.Message));
        }

        await _workOrders.AddAsync(workOrder, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("WorkOrderCreated", nameof(MaintenanceWorkOrder), workOrder.Id.ToString(),
            new { workOrder.WorkOrderNo, request.TargetType }, ct);

        return Result.Success(MapWorkOrder(workOrder));
    }

    public async Task<Result<WorkOrderDto>> UpdateWorkOrderAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<WorkOrderDto>(Error.Validation(validation.ToMessage()));
        }

        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure<WorkOrderDto>(Error.NotFound($"Work order {id} was not found."));
        }

        try
        {
            workOrder.UpdateDetails(request.Title, request.Description);
            workOrder.SetLaborCost(request.LaborCost);
            if (request.ScheduledDate is { } scheduled)
            {
                workOrder.Schedule(scheduled);
            }
        }
        catch (InvalidWorkOrderStateException ex)
        {
            return Result.Failure<WorkOrderDto>(Error.Conflict(ex.Message));
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkOrderDto>(Error.Validation(ex.Message));
        }

        _workOrders.Update(workOrder);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("WorkOrderUpdated", nameof(MaintenanceWorkOrder), workOrder.Id.ToString(), null, ct);

        return Result.Success(MapWorkOrder(workOrder));
    }

    public async Task<Result> StartWorkOrderAsync(Guid id, CancellationToken ct = default)
    {
        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Work order {id} was not found."));
        }

        try
        {
            workOrder.Start(_clock.UtcNow);
        }
        catch (InvalidWorkOrderStateException ex)
        {
            return Result.Failure(Error.Conflict(ex.Message));
        }

        _workOrders.Update(workOrder);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("WorkOrderStarted", nameof(MaintenanceWorkOrder), workOrder.Id.ToString(), null, ct);
        return Result.Success();
    }

    public async Task<Result<Guid>> AddTaskAsync(Guid id, AddTaskRequest request, CancellationToken ct = default)
    {
        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure<Guid>(Error.NotFound($"Work order {id} was not found."));
        }

        MaintenanceTask task;
        try
        {
            task = workOrder.AddTask(request.Description, request.LaborHours);
        }
        catch (DomainException ex)
        {
            return Result.Failure<Guid>(Error.Validation(ex.Message));
        }

        await _tasks.AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(task.Id);
    }

    public async Task<Result<WorkOrderPartDto>> AddPartAsync(Guid id, AddWorkOrderPartRequest request, CancellationToken ct = default)
    {
        var validation = await _addPartValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<WorkOrderPartDto>(Error.Validation(validation.ToMessage()));
        }

        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure<WorkOrderPartDto>(Error.NotFound($"Work order {id} was not found."));
        }
        if (workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            return Result.Failure<WorkOrderPartDto>(Error.Conflict($"Cannot add parts to a {workOrder.Status} work order."));
        }

        var part = await _parts.GetByIdAsync(request.PartId, ct);
        if (part is null || part.IsDeleted)
        {
            return Result.Failure<WorkOrderPartDto>(Error.NotFound($"Part {request.PartId} was not found."));
        }

        var unitCost = request.UnitCost ?? part.UnitCost;
        if (part.QuantityInStock < request.Quantity)
        {
            return Result.Failure<WorkOrderPartDto>(Error.Conflict(
                $"Insufficient stock for {part.PartNo}: available {part.QuantityInStock}, requested {request.Quantity}."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        // 1) Decrease stock (domain blocks negative as a safety net).
        part.DecreaseStock(request.Quantity);

        // 2) Stock OUT movement, balance stamped from the new part level.
        var movement = StockMovement.Out(part.Id, request.Quantity, unitCost, workOrderId: workOrder.Id, note: "Consumed by work order");
        movement.StampBalance(part.QuantityInStock);
        movement.AssignNumber(await _numbers.NextStockMovementNoAsync(ct));
        await _movements.AddAsync(movement, ct);

        // 3) Work-order part line, linked 1:1 to the movement; recomputes parts cost.
        var line = workOrder.AddPart(part.Id, request.Quantity, unitCost, movement.Id);
        await _workOrderParts.AddAsync(line, ct);
        _parts.Update(part);
        _workOrders.Update(workOrder);

        if (part.IsBelowMinimum)
        {
            await _alerts.EnsureCriticalStockAlertAsync(part.Id, part.Name, part.QuantityInStock, part.MinimumStock, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("WorkOrderPartAdded", nameof(WorkOrderPart), line.Id.ToString(),
            new { workOrder.WorkOrderNo, part.PartNo, request.Quantity }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapLine(line));
    }

    public async Task<Result<WorkOrderDto>> CompleteWorkOrderAsync(Guid id, CompleteWorkOrderRequest request, CancellationToken ct = default)
    {
        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure<WorkOrderDto>(Error.NotFound($"Work order {id} was not found."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        try
        {
            workOrder.Complete(request.OdometerAfterKm, _clock.UtcNow);
        }
        catch (InvalidWorkOrderStateException ex)
        {
            return Result.Failure<WorkOrderDto>(Error.Conflict(ex.Message));
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkOrderDto>(Error.Validation(ex.Message));
        }

        _workOrders.Update(workOrder);

        // Roll the vehicle odometer forward + reschedule maintenance.
        if (workOrder.TargetType == TargetType.Vehicle && workOrder.VehicleId is { } vehicleId && request.OdometerAfterKm is { } odometer)
        {
            var vehicle = await _vehicles.GetByIdAsync(vehicleId, ct);
            if (vehicle is not null)
            {
                vehicle.RecordMaintenanceCompletion(odometer, DateOnly.FromDateTime(_clock.UtcNow));
                _vehicles.Update(vehicle);
            }
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("WorkOrderCompleted", nameof(MaintenanceWorkOrder), workOrder.Id.ToString(),
            new { workOrder.WorkOrderNo, workOrder.TotalCost }, ct);
        await tx.CommitAsync(ct);

        // PDF report is IO, generated AFTER the DB transaction commits (best-effort;
        // can be regenerated on demand). Completion never depends on PDF success.
        try
        {
            await _reportService.GenerateMaintenanceReportAsync(workOrder.Id, ct);
        }
        catch
        {
            // Swallowed intentionally: the work order is already completed and audited.
        }

        return Result.Success(MapWorkOrder(workOrder));
    }

    public async Task<Result<WorkOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var workOrder = await _workOrders.GetByIdAsync(id, ct);
        if (workOrder is null || workOrder.IsDeleted)
        {
            return Result.Failure<WorkOrderDto>(Error.NotFound($"Work order {id} was not found."));
        }

        var lines = await _workOrderParts.ListAsync(p => p.WorkOrderId == id, ct);
        return Result.Success(MapWorkOrder(workOrder, lines));
    }

    public async Task<Result<PagedList<WorkOrderDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var (items, total) = await _workOrders.PagedAsync(w => !w.IsDeleted, request.Skip, request.PageSize, ct);
        var dtos = items.Select(w => MapWorkOrder(w)).ToList();
        return Result.Success(new PagedList<WorkOrderDto>(dtos, total, request.Page, request.PageSize));
    }

    private static WorkOrderDto MapWorkOrder(MaintenanceWorkOrder w, IReadOnlyList<WorkOrderPart>? lines = null)
    {
        IReadOnlyList<WorkOrderPartDto> parts = (lines ?? []).Select(MapLine).ToList();
        return new WorkOrderDto(
            w.Id, w.WorkOrderNo, w.TargetType.ToString(), w.VehicleId, w.TrailerId,
            w.Status.ToString(), w.MaintenanceType.ToString(), w.Title, w.Description,
            w.OdometerBeforeKm, w.OdometerAfterKm, w.LaborCost, w.PartsCost, w.TotalCost,
            w.ScheduledDate, w.CompletedAt, parts);
    }

    private static WorkOrderPartDto MapLine(WorkOrderPart l) =>
        new(l.Id, l.PartId, l.Quantity, l.UnitCost, l.LineTotal);
}
