using ECS.Api.Common.Authorization;
using ECS.Application.Features.Trailers;
using ECS.Application.Features.Trailers.Dtos;
using ECS.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class TrailersController : ApiControllerBase
{
    private readonly ITrailerService _service;

    public TrailersController(ITrailerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] bool sortDescending = false,
        CancellationToken ct = default)
        => RespondPaged(await _service.GetPagedAsync(new PagedQuery(page, pageSize, search, sortBy, sortDescending), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Respond(await _service.GetByIdAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTrailerRequest request, CancellationToken ct)
        => Respond(await _service.CreateAsync(request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTrailerRequest request, CancellationToken ct)
        => Respond(await _service.UpdateAsync(id, request, ct));

    [Authorize(Policy = ApiPolicies.ManageFleet)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Respond(await _service.DeleteAsync(id, ct));
}
