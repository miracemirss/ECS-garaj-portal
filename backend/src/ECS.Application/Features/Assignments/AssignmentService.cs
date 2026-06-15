using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Assignments.Dtos;
using ECS.Domain.Entities;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Assignments;

public sealed class AssignmentService : IAssignmentService
{
    private readonly IRepository<VehicleTrailerAssignment> _vehicleTrailer;
    private readonly IRepository<DriverVehicleAssignment> _driverVehicle;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Trailer> _trailers;
    private readonly IRepository<Driver> _drivers;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IValidator<AssignVehicleTrailerRequest> _vtValidator;
    private readonly IValidator<AssignDriverVehicleRequest> _dvValidator;

    public AssignmentService(
        IRepository<VehicleTrailerAssignment> vehicleTrailer,
        IRepository<DriverVehicleAssignment> driverVehicle,
        IRepository<Vehicle> vehicles,
        IRepository<Trailer> trailers,
        IRepository<Driver> drivers,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IValidator<AssignVehicleTrailerRequest> vtValidator,
        IValidator<AssignDriverVehicleRequest> dvValidator)
    {
        _vehicleTrailer = vehicleTrailer;
        _driverVehicle = driverVehicle;
        _vehicles = vehicles;
        _trailers = trailers;
        _drivers = drivers;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _vtValidator = vtValidator;
        _dvValidator = dvValidator;
    }

    public async Task<Result<VehicleTrailerAssignmentDto>> AssignVehicleTrailerAsync(AssignVehicleTrailerRequest request, CancellationToken ct = default)
    {
        var validation = await _vtValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<VehicleTrailerAssignmentDto>(Error.Validation(validation.ToMessage()));
        }

        if (!await _vehicles.AnyAsync(v => v.Id == request.VehicleId && !v.IsDeleted, ct))
        {
            return Result.Failure<VehicleTrailerAssignmentDto>(Error.NotFound($"Vehicle {request.VehicleId} was not found."));
        }
        if (!await _trailers.AnyAsync(t => t.Id == request.TrailerId && !t.IsDeleted, ct))
        {
            return Result.Failure<VehicleTrailerAssignmentDto>(Error.NotFound($"Trailer {request.TrailerId} was not found."));
        }

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        await using var tx = await _uow.BeginTransactionAsync(ct);

        // 1) Close active links on either side and FLUSH, so the partial unique
        //    index never sees two active rows during the insert below.
        var actives = await _vehicleTrailer.ListAsync(
            a => a.EndedAt == null && (a.VehicleId == request.VehicleId || a.TrailerId == request.TrailerId), ct);
        foreach (var active in actives)
        {
            active.End(now, userId);
            _vehicleTrailer.Update(active);
        }
        if (actives.Count > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        // 2) Open the new active link.
        var assignment = VehicleTrailerAssignment.Start(request.VehicleId, request.TrailerId, now, request.Note);
        await _vehicleTrailer.AddAsync(assignment, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("VehicleTrailerAssigned", nameof(VehicleTrailerAssignment), assignment.Id.ToString(),
            new { request.VehicleId, request.TrailerId }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapVt(assignment));
    }

    public async Task<Result> EndVehicleTrailerAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _vehicleTrailer.GetByIdAsync(assignmentId, ct);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound($"Assignment {assignmentId} was not found."));
        }
        if (!assignment.IsActive)
        {
            return Result.Failure(Error.Conflict("Assignment is already ended."));
        }

        assignment.End(_clock.UtcNow, _currentUser.UserId);
        _vehicleTrailer.Update(assignment);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("VehicleTrailerAssignmentEnded", nameof(VehicleTrailerAssignment), assignment.Id.ToString(), null, ct);
        return Result.Success();
    }

    public async Task<Result<VehicleTrailerAssignmentDto>> GetActiveVehicleTrailerAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var active = await _vehicleTrailer.FirstOrDefaultAsync(a => a.VehicleId == vehicleId && a.EndedAt == null, ct);
        return active is null
            ? Result.Failure<VehicleTrailerAssignmentDto>(Error.NotFound($"No active trailer for vehicle {vehicleId}."))
            : Result.Success(MapVt(active));
    }

    public async Task<Result<DriverVehicleAssignmentDto>> AssignDriverVehicleAsync(AssignDriverVehicleRequest request, CancellationToken ct = default)
    {
        var validation = await _dvValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<DriverVehicleAssignmentDto>(Error.Validation(validation.ToMessage()));
        }

        if (!await _drivers.AnyAsync(d => d.Id == request.DriverId && !d.IsDeleted, ct))
        {
            return Result.Failure<DriverVehicleAssignmentDto>(Error.NotFound($"Driver {request.DriverId} was not found."));
        }
        if (!await _vehicles.AnyAsync(v => v.Id == request.VehicleId && !v.IsDeleted, ct))
        {
            return Result.Failure<DriverVehicleAssignmentDto>(Error.NotFound($"Vehicle {request.VehicleId} was not found."));
        }

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        await using var tx = await _uow.BeginTransactionAsync(ct);

        var actives = await _driverVehicle.ListAsync(
            a => a.EndedAt == null && (a.DriverId == request.DriverId || a.VehicleId == request.VehicleId), ct);
        foreach (var active in actives)
        {
            active.End(now, userId);
            _driverVehicle.Update(active);
        }
        if (actives.Count > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        var assignment = DriverVehicleAssignment.Start(request.DriverId, request.VehicleId, now, request.Note);
        await _driverVehicle.AddAsync(assignment, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("DriverVehicleAssigned", nameof(DriverVehicleAssignment), assignment.Id.ToString(),
            new { request.DriverId, request.VehicleId }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapDv(assignment));
    }

    public async Task<Result> EndDriverVehicleAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _driverVehicle.GetByIdAsync(assignmentId, ct);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound($"Assignment {assignmentId} was not found."));
        }
        if (!assignment.IsActive)
        {
            return Result.Failure(Error.Conflict("Assignment is already ended."));
        }

        assignment.End(_clock.UtcNow, _currentUser.UserId);
        _driverVehicle.Update(assignment);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DriverVehicleAssignmentEnded", nameof(DriverVehicleAssignment), assignment.Id.ToString(), null, ct);
        return Result.Success();
    }

    public async Task<Result<DriverVehicleAssignmentDto>> GetActiveDriverVehicleAsync(Guid driverId, CancellationToken ct = default)
    {
        var active = await _driverVehicle.FirstOrDefaultAsync(a => a.DriverId == driverId && a.EndedAt == null, ct);
        return active is null
            ? Result.Failure<DriverVehicleAssignmentDto>(Error.NotFound($"No active vehicle for driver {driverId}."))
            : Result.Success(MapDv(active));
    }

    private static VehicleTrailerAssignmentDto MapVt(VehicleTrailerAssignment a) =>
        new(a.Id, a.VehicleId, a.TrailerId, a.Status.ToString(), a.StartedAt, a.EndedAt);

    private static DriverVehicleAssignmentDto MapDv(DriverVehicleAssignment a) =>
        new(a.Id, a.DriverId, a.VehicleId, a.Status.ToString(), a.StartedAt, a.EndedAt);
}
