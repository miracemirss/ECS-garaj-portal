namespace ECS.Application.Features.Trailers.Dtos;

public sealed record TrailerDto(
    Guid Id,
    string PlateNo,
    string? TrailerType,
    string? Brand,
    string Status,
    decimal? CapacityKg);

public sealed record CreateTrailerRequest(
    string PlateNo,
    string? TrailerType = null,
    string? Brand = null,
    decimal? CapacityKg = null);

public sealed record UpdateTrailerRequest(
    string? TrailerType = null,
    string? Brand = null,
    string? Model = null,
    decimal? CapacityKg = null);
