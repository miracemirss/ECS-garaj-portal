using System.Linq.Expressions;
using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Vehicles.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using ECS.Domain.ValueObjects;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreateVehicleRequest> _createValidator;
    private readonly IValidator<UpdateVehicleRequest> _updateValidator;

    public VehicleService(
        IRepository<Vehicle> vehicles,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IValidator<CreateVehicleRequest> createValidator,
        IValidator<UpdateVehicleRequest> updateValidator)
    {
        _vehicles = vehicles;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<VehicleDto>(Error.Validation(validation.ToMessage()));
        }

        Vehicle vehicle;
        try
        {
            vehicle = Vehicle.Create(request.PlateNo, request.Brand, request.Model, request.ModelYear, request.Vin);
        }
        catch (DomainException ex)
        {
            return Result.Failure<VehicleDto>(Error.Validation(ex.Message));
        }

        if (await _vehicles.AnyAsync(v => v.PlateNo == vehicle.PlateNo, ct))
        {
            return Result.Failure<VehicleDto>(Error.Conflict($"A vehicle with plate {vehicle.PlateNo} already exists."));
        }

        await _vehicles.AddAsync(vehicle, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("VehicleCreated", nameof(Vehicle), vehicle.Id.ToString(), new { vehicle.PlateNo }, ct);

        return Result.Success(MapToDto(vehicle));
    }

    public async Task<Result<VehicleDto>> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<VehicleDto>(Error.Validation(validation.ToMessage()));
        }

        var vehicle = await _vehicles.GetByIdAsync(id, ct);
        if (vehicle is null || vehicle.IsDeleted)
        {
            return Result.Failure<VehicleDto>(Error.NotFound($"Vehicle {id} was not found."));
        }

        try
        {
            vehicle.UpdateDetails(request.Brand, request.Model, request.ModelYear, request.Color);
            vehicle.SetMaintenancePlan(request.MaintenanceIntervalKm, request.MaintenanceIntervalDays);
        }
        catch (DomainException ex)
        {
            return Result.Failure<VehicleDto>(Error.Validation(ex.Message));
        }

        _vehicles.Update(vehicle);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("VehicleUpdated", nameof(Vehicle), vehicle.Id.ToString(), null, ct);

        return Result.Success(MapToDto(vehicle));
    }

    public async Task<Result<VehicleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, ct);
        return vehicle is null || vehicle.IsDeleted
            ? Result.Failure<VehicleDto>(Error.NotFound($"Vehicle {id} was not found."))
            : Result.Success(MapToDto(vehicle));
    }

    public async Task<Result<PagedList<VehicleDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        Expression<Func<Vehicle, bool>> predicate = search is null
            ? v => !v.IsDeleted
            : v => !v.IsDeleted && (v.PlateNo.Contains(search) || v.Brand.Contains(search));

        Expression<Func<Vehicle, object>>? orderBy = query.SortBy?.ToLowerInvariant() switch
        {
            "plateno" => v => v.PlateNo,
            "brand" => v => v.Brand,
            "status" => v => v.Status,
            "odometer" => v => v.CurrentOdometerKm,
            "createdat" => v => v.CreatedAt,
            _ => null
        };

        var (items, total) = await _vehicles.PagedAsync(predicate, orderBy, query.SortDescending, query.Skip, query.PageSize, ct);
        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new PagedList<VehicleDto>(dtos, total, query.Page, query.PageSize));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, ct);
        if (vehicle is null || vehicle.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Vehicle {id} was not found."));
        }

        vehicle.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        _vehicles.Update(vehicle);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("VehicleDeleted", nameof(Vehicle), vehicle.Id.ToString(), null, ct);

        return Result.Success();
    }

    private static VehicleDto MapToDto(Vehicle v) => new(
        v.Id, v.PlateNo, v.Vin, v.Brand, v.Model, v.ModelYear,
        v.Status.ToString(), v.CurrentOdometerKm, v.NextMaintenanceKm, v.NextMaintenanceDate);
}
