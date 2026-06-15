using ECS.Application.Features.Maintenance.Dtos;
using ECS.Shared.Pagination;
using ECS.Shared.Results;

namespace ECS.Application.Features.Maintenance;

public interface IMaintenanceService
{
    Task<Result<WorkOrderDto>> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> UpdateWorkOrderAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken ct = default);
    Task<Result> StartWorkOrderAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> AddTaskAsync(Guid id, AddTaskRequest request, CancellationToken ct = default);

    /// <summary>Transactional: stock decrement + StockMovement + work-order part line (+ critical-stock alert) commit together.</summary>
    Task<Result<WorkOrderPartDto>> AddPartAsync(Guid id, AddWorkOrderPartRequest request, CancellationToken ct = default);

    /// <summary>Transactional: complete + odometer/next-maintenance update + cost finalize; PDF report generated after commit.</summary>
    Task<Result<WorkOrderDto>> CompleteWorkOrderAsync(Guid id, CompleteWorkOrderRequest request, CancellationToken ct = default);

    Task<Result<WorkOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<WorkOrderDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default);
}
