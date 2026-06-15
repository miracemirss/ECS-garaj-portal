using ECS.Application.Features.Vehicles.Dtos;
using ECS.Shared.Pagination;
using ECS.Shared.Results;

namespace ECS.Application.Features.Vehicles;

public interface IVehicleService
{
    Task<Result<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default);
    Task<Result<VehicleDto>> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct = default);
    Task<Result<VehicleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<VehicleDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
