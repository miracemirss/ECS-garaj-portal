using ECS.Api.Common.Authorization;
using ECS.Application.Features.Inventory;
using ECS.Application.Features.Inventory.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize(Policy = ApiPolicies.ManageInventory)]
public sealed class StockMovementsController : ApiControllerBase
{
    private readonly IInventoryService _service;

    public StockMovementsController(IInventoryService service) => _service = service;

    [HttpPost("receive")]
    public async Task<IActionResult> Receive(ReceiveStockRequest request, CancellationToken ct)
        => Respond(await _service.ReceiveStockAsync(request, ct), StatusCodes.Status201Created);

    [HttpPost("issue")]
    public async Task<IActionResult> Issue(IssueStockRequest request, CancellationToken ct)
        => Respond(await _service.IssueStockAsync(request, ct), StatusCodes.Status201Created);

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustStockRequest request, CancellationToken ct)
        => Respond(await _service.AdjustStockAsync(request, ct), StatusCodes.Status201Created);
}
