namespace ECS.Application.Features.Vehicles.Dtos;

public sealed record VehicleDto(
    Guid Id,
    string PlateNo,
    string? Vin,
    string Brand,
    string? Model,
    int? ModelYear,
    string Status,
    int CurrentOdometerKm,
    int? NextMaintenanceKm,
    DateOnly? NextMaintenanceDate);

public sealed record CreateVehicleRequest(
    string PlateNo,
    string Brand,
    string? Model = null,
    int? ModelYear = null,
    string? Vin = null);

public sealed record UpdateVehicleRequest(
    string Brand,
    string? Model = null,
    int? ModelYear = null,
    string? Color = null,
    int? MaintenanceIntervalKm = null,
    int? MaintenanceIntervalDays = null);
