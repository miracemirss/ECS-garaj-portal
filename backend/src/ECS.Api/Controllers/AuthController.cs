using ECS.Application.Features.Auth;
using ECS.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

[AllowAnonymous]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        => Respond(await _auth.LoginAsync(request, Ip, ct));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
        => Respond(await _auth.RefreshAsync(request, Ip, ct));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
        => Respond(await _auth.LogoutAsync(request, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Respond(await _auth.GetCurrentAsync(ct));
}
