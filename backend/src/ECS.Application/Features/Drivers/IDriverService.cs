using ECS.Application.Features.Drivers.Dtos;
using ECS.Shared.Pagination;
using ECS.Shared.Results;

namespace ECS.Application.Features.Drivers;

public interface IDriverService
{
    Task<Result<DriverDto>> CreateAsync(CreateDriverRequest request, CancellationToken ct = default);
    Task<Result<DriverDto>> UpdateAsync(Guid id, UpdateDriverRequest request, CancellationToken ct = default);
    Task<Result<DriverDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<DriverDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
