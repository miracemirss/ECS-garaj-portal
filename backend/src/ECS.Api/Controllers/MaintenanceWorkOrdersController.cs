using ECS.Api.Common.Authorization;
using ECS.Application.Features.Maintenance;
using ECS.Application.Features.Maintenance.Dtos;
using ECS.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class MaintenanceWorkOrdersController : ApiControllerBase
{
    private readonly IMaintenanceService _service;

    public MaintenanceWorkOrdersController(IMaintenanceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => RespondPaged(await _service.GetPagedAsync(new PaginationRequest(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Respond(await _service.GetByIdAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkOrderRequest request, CancellationToken ct)
        => Respond(await _service.CreateWorkOrderAsync(request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkOrderRequest request, CancellationToken ct)
        => Respond(await _service.UpdateWorkOrderAsync(id, request, ct));

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
        => Respond(await _service.StartWorkOrderAsync(id, ct));

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> AddTask(Guid id, AddTaskRequest request, CancellationToken ct)
        => Respond(await _service.AddTaskAsync(id, request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPost("{id:guid}/parts")]
    public async Task<IActionResult> AddPart(Guid id, AddWorkOrderPartRequest request, CancellationToken ct)
        => Respond(await _service.AddPartAsync(id, request, ct), StatusCodes.Status201Created);

    [Authorize(Policy = ApiPolicies.ManageMaintenance)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteWorkOrderRequest request, CancellationToken ct)
        => Respond(await _service.CompleteWorkOrderAsync(id, request, ct));
}
