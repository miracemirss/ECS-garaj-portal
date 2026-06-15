namespace ECS.Application.Features.Trailers.Dtos;

public sealed record TrailerDto(
    Guid Id,
    string PlateNo,
    string? Vin,
    string? TrailerType,
    string? Brand,
    string Status,
    decimal? CapacityKg,
    int? TireConditionPercent);

public sealed record CreateTrailerRequest(
    string PlateNo,
    string? TrailerType = null,
    string? Brand = null,
    decimal? CapacityKg = null,
    string? Vin = null,
    int? TireConditionPercent = null);

public sealed record UpdateTrailerRequest(
    string? TrailerType = null,
    string? Brand = null,
    string? Model = null,
    decimal? CapacityKg = null,
    int? TireConditionPercent = null);
