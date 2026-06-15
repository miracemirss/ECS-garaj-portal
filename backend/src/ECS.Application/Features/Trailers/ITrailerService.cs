using ECS.Application.Features.Trailers.Dtos;
using ECS.Shared.Pagination;
using ECS.Shared.Results;

namespace ECS.Application.Features.Trailers;

public interface ITrailerService
{
    Task<Result<TrailerDto>> CreateAsync(CreateTrailerRequest request, CancellationToken ct = default);
    Task<Result<TrailerDto>> UpdateAsync(Guid id, UpdateTrailerRequest request, CancellationToken ct = default);
    Task<Result<TrailerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<TrailerDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
