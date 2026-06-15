using ECS.Api.Common.Authorization;
using ECS.Application.Features.Settings;
using ECS.Application.Features.Settings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[Authorize]
public sealed class SettingsController : ApiControllerBase
{
    private readonly ISettingsService _service;

    public SettingsController(ISettingsService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Respond(await _service.GetAsync(ct));

    [Authorize(Policy = ApiPolicies.AdminOnly)]
    [HttpPut]
    public async Task<IActionResult> Update(UpdateSettingsRequest request, CancellationToken ct)
        => Respond(await _service.UpdateAsync(request, ct));
}
