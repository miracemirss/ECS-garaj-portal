using ECS.Api.Common.Authorization;
using ECS.Application.Features.Assignments;
using ECS.Application.Features.Assignments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize(Policy = ApiPolicies.ManageFleet)]
public sealed class VehicleTrailerAssignmentsController : ApiControllerBase
{
    private readonly IAssignmentService _service;

    public VehicleTrailerAssignmentsController(IAssignmentService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Assign(AssignVehicleTrailerRequest request, CancellationToken ct)
        => Respond(await _service.AssignVehicleTrailerAsync(request, ct), StatusCodes.Status201Created);

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id, CancellationToken ct)
        => Respond(await _service.EndVehicleTrailerAssignmentAsync(id, ct));

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] Guid vehicleId, CancellationToken ct)
        => Respond(await _service.GetActiveVehicleTrailerAsync(vehicleId, ct));
}
