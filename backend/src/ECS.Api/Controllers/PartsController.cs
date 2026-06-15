using ECS.Api.Common.Authorization;
using ECS.Application.Features.Inventory;
using ECS.Application.Features.Inventory.Dtos;
using ECS.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class PartsController : ApiControllerBase
{
    private readonly IInventoryService _service;

    public PartsController(IInventoryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] bool sortDescending = false,
        CancellationToken ct = default)
        => RespondPaged(await _service.GetPartsPagedAsync(new PagedQuery(page, pageSize, search, sortBy, sortDescending), ct));

    [HttpGet("critical")]
    public async Task<IActionResult> GetCritical(CancellationToken ct)
        => Respond(await _service.GetCriticalStocksAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Respond(await _service.GetPartAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageInventory)]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePartRequest request, CancellationToken ct)
        => Respond(await _service.CreatePartAsync(request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageInventory)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePartRequest request, CancellationToken ct)
        => Respond(await _service.UpdatePartAsync(id, request, ct));
}
