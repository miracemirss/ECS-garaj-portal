using ECS.Application.Features.Inventory.Dtos;
using ECS.Shared.Pagination;
using ECS.Shared.Results;

namespace ECS.Application.Features.Inventory;

public interface IInventoryService
{
    Task<Result<PartDto>> CreatePartAsync(CreatePartRequest request, CancellationToken ct = default);
    Task<Result<PartDto>> UpdatePartAsync(Guid id, UpdatePartRequest request, CancellationToken ct = default);
    Task<Result<PartDto>> GetPartAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<PartDto>>> GetPartsPagedAsync(PagedQuery query, CancellationToken ct = default);

    // Each stock operation runs in a transaction: movement + part balance (+ alert) commit together.
    Task<Result<StockMovementDto>> ReceiveStockAsync(ReceiveStockRequest request, CancellationToken ct = default);
    Task<Result<StockMovementDto>> IssueStockAsync(IssueStockRequest request, CancellationToken ct = default);
    Task<Result<StockMovementDto>> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PartDto>>> GetCriticalStocksAsync(CancellationToken ct = default);
}
