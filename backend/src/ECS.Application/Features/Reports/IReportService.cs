using ECS.Application.Features.Reports.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Reports;

public interface IReportService
{
    /// <summary>Renders (or re-renders) the maintenance PDF for a completed work order and stores it.</summary>
    Task<Result<ReportFileDto>> GenerateMaintenanceReportAsync(Guid workOrderId, CancellationToken ct = default);

    /// <summary>Lists the report files already generated for a work order (newest first).</summary>
    Task<Result<IReadOnlyList<ReportFileDto>>> GetReportsForWorkOrderAsync(Guid workOrderId, CancellationToken ct = default);

    /// <summary>Loads a stored report's binary content for download. Re-generates on demand if the file is missing.</summary>
    Task<Result<ReportFileContentDto>> GetReportFileAsync(Guid reportFileId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<VehicleCostSummaryDto>>> GetVehicleCostSummariesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<MonthlyCostDto>>> GetMonthlyCostsAsync(CancellationToken ct = default);
}
