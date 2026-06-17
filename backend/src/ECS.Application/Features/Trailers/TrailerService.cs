using System.Linq.Expressions;
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
            trailer = Trailer.Create(request.PlateNo, request.TrailerType, request.Brand, request.CapacityKg, request.Vin, request.TireConditionPercent);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TrailerDto>(Error.Validation(ex.Message));
        }

        if (await _trailers.AnyAsync(t => t.PlateNo == trailer.PlateNo && !t.IsDeleted, ct))
        {
            return Result.Failure<TrailerDto>(Error.Conflict("Bu plakaya sahip dorse zaten kayıtlı."));
        }
        if (trailer.Vin is not null && await _trailers.AnyAsync(t => t.Vin == trailer.Vin && !t.IsDeleted, ct))
        {
            return Result.Failure<TrailerDto>(Error.Conflict("Bu şasi numarasına sahip dorse zaten kayıtlı."));
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
            return Result.Failure<TrailerDto>(Error.NotFound("Dorse bulunamadı."));
        }

        try
        {
            trailer.UpdateDetails(request.TrailerType, request.Brand, request.Model, request.CapacityKg, request.TireConditionPercent);
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
            ? Result.Failure<TrailerDto>(Error.NotFound("Dorse bulunamadı."))
            : Result.Success(MapToDto(trailer));
    }

    public async Task<Result<PagedList<TrailerDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        Expression<Func<Trailer, bool>> predicate = search is null
            ? t => !t.IsDeleted
            : t => !t.IsDeleted && (t.PlateNo.Contains(search) || (t.TrailerType != null && t.TrailerType.Contains(search)));

        Expression<Func<Trailer, object>>? orderBy = query.SortBy?.ToLowerInvariant() switch
        {
            "plateno" => t => t.PlateNo,
            "trailertype" => t => t.TrailerType!,
            "status" => t => t.Status,
            "createdat" => t => t.CreatedAt,
            _ => null
        };

        var (items, total) = await _trailers.PagedAsync(predicate, orderBy, query.SortDescending, query.Skip, query.PageSize, ct);
        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new PagedList<TrailerDto>(dtos, total, query.Page, query.PageSize));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trailer = await _trailers.GetByIdAsync(id, ct);
        if (trailer is null || trailer.IsDeleted)
        {
            return Result.Failure(Error.NotFound("Dorse bulunamadı."));
        }

        trailer.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        _trailers.Update(trailer);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("TrailerDeleted", nameof(Trailer), trailer.Id.ToString(), null, ct);

        return Result.Success();
    }

    private static TrailerDto MapToDto(Trailer t) => new(
        t.Id, t.PlateNo, t.Vin, t.TrailerType, t.Brand, t.Status.ToString(), t.CapacityKg, t.TireConditionPercent);
}
