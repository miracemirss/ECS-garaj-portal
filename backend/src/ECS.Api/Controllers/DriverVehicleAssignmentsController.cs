using ECS.Api.Common.Authorization;
using ECS.Application.Features.Assignments;
using ECS.Application.Features.Assignments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize(Policy = ApiPolicies.ManageFleet)]
public sealed class DriverVehicleAssignmentsController : ApiControllerBase
{
    private readonly IAssignmentService _service;

    public DriverVehicleAssignmentsController(IAssignmentService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Assign(AssignDriverVehicleRequest request, CancellationToken ct)
        => Respond(await _service.AssignDriverVehicleAsync(request, ct), StatusCodes.Status201Created);

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id, CancellationToken ct)
        => Respond(await _service.EndDriverVehicleAssignmentAsync(id, ct));

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] Guid driverId, CancellationToken ct)
        => Respond(await _service.GetActiveDriverVehicleAsync(driverId, ct));
}
