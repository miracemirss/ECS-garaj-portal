using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Trailers.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Trailers;

public sealed class TrailerService : ITrailerService
{
    private readonly IRepository<Trailer> _trailers;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreateTrailerRequest> _createValidator;
    private readonly IValidator<UpdateTrailerRequest> _updateValidator;

    public TrailerService(
        IRepository<Trailer> trailers,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IValidator<CreateTrailerRequest> createValidator,
        IValidator<UpdateTrailerRequest> updateValidator)
    {
        _trailers = trailers;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<TrailerDto>> CreateAsync(CreateTrailerRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<TrailerDto>(Error.Validation(validation.ToMessage()));
        }

        Trailer trailer;
        try
        {
            trailer = Trailer.Create(request.PlateNo, request.TrailerType, request.Brand, request.CapacityKg);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TrailerDto>(Error.Validation(ex.Message));
        }

        if (await _trailers.AnyAsync(t => t.PlateNo == trailer.PlateNo, ct))
        {
            return Result.Failure<TrailerDto>(Error.Conflict($"A trailer with plate {trailer.PlateNo} already exists."));
        }

        await _trailers.AddAsync(trailer, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("TrailerCreated", nameof(Trailer), trailer.Id.ToString(), new { trailer.PlateNo }, ct);

        return Result.Success(MapToDto(trailer));
    }

    public async Task<Result<TrailerDto>> UpdateAsync(Guid id, UpdateTrailerRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<TrailerDto>(Error.Validation(validation.ToMessage()));
        }

        var trailer = await _trailers.GetByIdAsync(id, ct);
        if (trailer is null || trailer.IsDeleted)
        {
            return Result.Failure<TrailerDto>(Error.NotFound($"Trailer {id} was not found."));
        }

        try
        {
            trailer.UpdateDetails(request.TrailerType, request.Brand, request.Model, request.CapacityKg);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TrailerDto>(Error.Validation(ex.Message));
        }

        _trailers.Update(trailer);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("TrailerUpdated", nameof(Trailer), trailer.Id.ToString(), null, ct);

        return Result.Success(MapToDto(trailer));
    }

    public async Task<Result<TrailerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trailer = await _trailers.GetByIdAsync(id, ct);
        return trailer is null || trailer.IsDeleted
            ? Result.Failure<TrailerDto>(Error.NotFound($"Trailer {id} was not found."))
            : Result.Success(MapToDto(trailer));
    }

    public async Task<Result<PagedList<TrailerDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var (items, total) = await _trailers.PagedAsync(t => !t.IsDeleted, request.Skip, request.PageSize, ct);
        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new PagedList<TrailerDto>(dtos, total, request.Page, request.PageSize));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trailer = await _trailers.GetByIdAsync(id, ct);
        if (trailer is null || trailer.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Trailer {id} was not found."));
        }

        trailer.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        _trailers.Update(trailer);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("TrailerDeleted", nameof(Trailer), trailer.Id.ToString(), null, ct);

        return Result.Success();
    }

    private static TrailerDto MapToDto(Trailer t) => new(
        t.Id, t.PlateNo, t.TrailerType, t.Brand, t.Status.ToString(), t.CapacityKg);
}
