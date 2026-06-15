using ECS.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

/// <summary>
/// Base controller that turns an Application <see cref="Result"/> into an HTTP
/// response. Feature controllers stay thin: call a service, return HandleResult.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? Ok() : ToProblem(result.Error);

    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);

    private ObjectResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "NotFound" => StatusCodes.Status404NotFound,
            "Validation" => StatusCodes.Status400BadRequest,
            "Conflict" => StatusCodes.Status409Conflict,
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(status, new { error.Code, error.Message });
    }
}
