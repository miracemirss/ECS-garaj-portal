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

    [HttpPost("maintenance/{workOrderId:guid}")]
    public async Task<IActionResult> GenerateMaintenance(Guid workOrderId, CancellationToken ct)
        => Respond(await _service.GenerateMaintenanceReportAsync(workOrderId, ct), StatusCodes.Status201Created);

    [HttpGet("vehicle-costs")]
    public async Task<IActionResult> VehicleCosts(CancellationToken ct)
        => Respond(await _service.GetVehicleCostSummariesAsync(ct));

    [HttpGet("monthly-costs")]
    public async Task<IActionResult> MonthlyCosts(CancellationToken ct)
        => Respond(await _service.GetMonthlyCostsAsync(ct));
}
