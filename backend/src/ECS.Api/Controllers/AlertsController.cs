using ECS.Api.Common.Authorization;
using ECS.Application.Features.Alerts;
using ECS.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class AlertsController : ApiControllerBase
{
    private readonly IAlertService _service;

    public AlertsController(IAlertService service) => _service = service;

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(CancellationToken ct)
        => Respond(await _service.GetOpenAsync(ct));

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
        => Respond(await _service.AcknowledgeAsync(id, ct));

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
        => Respond(await _service.ResolveAsync(id, ct));

    [HttpPost("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
        => Respond(await _service.DismissAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageInventory)]
    [HttpPost("generate/critical-stock")]
    public async Task<IActionResult> GenerateCriticalStock(CancellationToken ct)
        => Respond(Result.Success(await _service.GenerateCriticalStockAlertsAsync(ct)));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPost("generate/maintenance-due")]
    public async Task<IActionResult> GenerateMaintenanceDue(CancellationToken ct)
        => Respond(Result.Success(await _service.GenerateUpcomingMaintenanceAlertsAsync(ct)));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPost("generate/document-expiry")]
    public async Task<IActionResult> GenerateDocumentExpiry(CancellationToken ct)
        => Respond(Result.Success(await _service.GenerateDocumentExpiryAlertsAsync(ct)));
}
