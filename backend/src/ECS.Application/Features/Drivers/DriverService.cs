using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Drivers.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Drivers;

public sealed class DriverService : IDriverService
{
    private readonly IRepository<Driver> _drivers;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreateDriverRequest> _createValidator;
    private readonly IValidator<UpdateDriverRequest> _updateValidator;

    public DriverService(
        IRepository<Driver> drivers,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IValidator<CreateDriverRequest> createValidator,
        IValidator<UpdateDriverRequest> updateValidator)
    {
        _drivers = drivers;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<DriverDto>> CreateAsync(CreateDriverRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<DriverDto>(Error.Validation(validation.ToMessage()));
        }

        if (!string.IsNullOrWhiteSpace(request.NationalId)
            && await _drivers.AnyAsync(d => d.NationalId == request.NationalId, ct))
        {
            return Result.Failure<DriverDto>(Error.Conflict("A driver with this national id already exists."));
        }

        Driver driver;
        try
        {
            driver = Driver.Create(request.FirstName, request.LastName, request.NationalId);
            driver.SetContact(request.Phone, request.Email);
        }
        catch (DomainException ex)
        {
            return Result.Failure<DriverDto>(Error.Validation(ex.Message));
        }

        await _drivers.AddAsync(driver, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DriverCreated", nameof(Driver), driver.Id.ToString(), new { driver.FullName }, ct);

        return Result.Success(MapToDto(driver));
    }

    public async Task<Result<DriverDto>> UpdateAsync(Guid id, UpdateDriverRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<DriverDto>(Error.Validation(validation.ToMessage()));
        }

        var driver = await _drivers.GetByIdAsync(id, ct);
        if (driver is null || driver.IsDeleted)
        {
            return Result.Failure<DriverDto>(Error.NotFound($"Driver {id} was not found."));
        }

        driver.SetContact(request.Phone, request.Email);
        driver.SetLicense(request.LicenseNo, request.LicenseClass, request.LicenseExpiryDate);
        _drivers.Update(driver);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DriverUpdated", nameof(Driver), driver.Id.ToString(), null, ct);

        return Result.Success(MapToDto(driver));
    }

    public async Task<Result<DriverDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var driver = await _drivers.GetByIdAsync(id, ct);
        return driver is null || driver.IsDeleted
            ? Result.Failure<DriverDto>(Error.NotFound($"Driver {id} was not found."))
            : Result.Success(MapToDto(driver));
    }

    public async Task<Result<PagedList<DriverDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var (items, total) = await _drivers.PagedAsync(d => !d.IsDeleted, request.Skip, request.PageSize, ct);
        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new PagedList<DriverDto>(dtos, total, request.Page, request.PageSize));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var driver = await _drivers.GetByIdAsync(id, ct);
        if (driver is null || driver.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Driver {id} was not found."));
        }

        driver.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        _drivers.Update(driver);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DriverDeleted", nameof(Driver), driver.Id.ToString(), null, ct);

        return Result.Success();
    }

    private static DriverDto MapToDto(Driver d) => new(
        d.Id, d.FirstName, d.LastName, d.FullName, d.NationalId, d.Phone, d.Email,
        d.Status.ToString(), d.LicenseNo, d.LicenseExpiryDate);
}
