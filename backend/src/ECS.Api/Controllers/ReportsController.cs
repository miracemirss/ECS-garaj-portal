using ECS.Api.Common.Authorization;
using ECS.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize(Policy = ApiPolicies.ViewReports)]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    /// <summary>Generates (or re-generates) the maintenance PDF for a completed work order.</summary>
    [HttpPost("maintenance/{workOrderId:guid}")]
    public async Task<IActionResult> GenerateMaintenance(Guid workOrderId, CancellationToken ct)
        => Respond(await _service.GenerateMaintenanceReportAsync(workOrderId, ct), StatusCodes.Status201Created);

    /// <summary>Lists the report files already generated for a work order (newest first).</summary>
    [HttpGet("maintenance/{workOrderId:guid}/files")]
    public async Task<IActionResult> ListForWorkOrder(Guid workOrderId, CancellationToken ct)
        => Respond(await _service.GetReportsForWorkOrderAsync(workOrderId, ct));

    /// <summary>Downloads a previously generated report file (re-rendered on demand if the file was pruned).</summary>
    [HttpGet("files/{reportFileId:guid}/download")]
    public async Task<IActionResult> Download(Guid reportFileId, CancellationToken ct)
    {
        var result = await _service.GetReportFileAsync(reportFileId, ct);
        if (!result.IsSuccess)
        {
            return Respond(result);
        }

        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("vehicle-costs")]
    public async Task<IActionResult> VehicleCosts(CancellationToken ct)
        => Respond(await _service.GetVehicleCostSummariesAsync(ct));

    [HttpGet("monthly-costs")]
    public async Task<IActionResult> MonthlyCosts(CancellationToken ct)
        => Respond(await _service.GetMonthlyCostsAsync(ct));
}
