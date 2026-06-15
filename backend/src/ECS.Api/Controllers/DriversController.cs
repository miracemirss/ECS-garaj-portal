using ECS.Api.Common.Authorization;
using ECS.Application.Features.Drivers;
using ECS.Application.Features.Drivers.Dtos;
using ECS.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class DriversController : ApiControllerBase
{
    private readonly IDriverService _service;

    public DriversController(IDriverService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => RespondPaged(await _service.GetPagedAsync(new PaginationRequest(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Respond(await _service.GetByIdAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken ct)
        => Respond(await _service.CreateAsync(request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDriverRequest request, CancellationToken ct)
        => Respond(await _service.UpdateAsync(id, request, ct));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Respond(await _service.DeleteAsync(id, ct));
}
