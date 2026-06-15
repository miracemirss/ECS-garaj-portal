using ECS.Application.Features.Reports.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Reports;

public interface IReportService
{
    Task<Result<ReportFileDto>> GenerateMaintenanceReportAsync(Guid workOrderId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<VehicleCostSummaryDto>>> GetVehicleCostSummariesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<MonthlyCostDto>>> GetMonthlyCostsAsync(CancellationToken ct = default);
}
