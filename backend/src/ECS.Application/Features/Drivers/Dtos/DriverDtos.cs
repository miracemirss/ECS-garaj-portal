namespace ECS.Application.Features.Drivers.Dtos;

public sealed record DriverDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? NationalId,
    string? Phone,
    string? Email,
    string Status,
    string? LicenseNo,
    DateOnly? LicenseExpiryDate);

public sealed record CreateDriverRequest(
    string FirstName,
    string LastName,
    string? NationalId = null,
    string? Phone = null,
    string? Email = null);

public sealed record UpdateDriverRequest(
    string? Phone = null,
    string? Email = null,
    string? LicenseNo = null,
    string? LicenseClass = null,
    DateOnly? LicenseExpiryDate = null);
