using ECS.Api.Common.Models;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace ECS.Api.Controllers;

/// <summary>
/// Base controller that turns an Application <see cref="Result"/> into the standard
/// <see cref="ApiResponse"/> envelope. Controllers stay thin: call a service, return Respond(...).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string TraceId => HttpContext.TraceIdentifier;

    protected IActionResult Respond(Result result, int successStatus = StatusCodes.Status200OK)
        => result.IsSuccess
            ? StatusCode(successStatus, ApiResponse.Successful(null, successStatus, TraceId))
            : Problem(result.Error);

    protected IActionResult Respond<T>(Result<T> result, int successStatus = StatusCodes.Status200OK)
        => result.IsSuccess
            ? StatusCode(successStatus, ApiResponse<T>.Successful(result.Value, null, successStatus, TraceId))
            : Problem(result.Error);

    protected IActionResult RespondPaged<T>(Result<PagedList<T>> result)
        => result.IsSuccess
            ? StatusCode(StatusCodes.Status200OK,
                ApiResponse<PagedResponse<T>>.Successful(PagedResponse<T>.From(result.Value), null, StatusCodes.Status200OK, TraceId))
            : Problem(result.Error);

    private IActionResult Problem(Error error)
    {
        var status = StatusFor(error.Code);
        object? errors = error.Code == "Validation"
            ? error.Message.Split("; ", StringSplitOptions.RemoveEmptyEntries)
            : null;
        return StatusCode(status, ApiResponse.Failure(error.Message, errors, status, TraceId));
    }

    private static int StatusFor(string code) => code switch
    {
        "NotFound" => StatusCodes.Status404NotFound,
        "Validation" => StatusCodes.Status400BadRequest,
        "Conflict" => StatusCodes.Status409Conflict,
        "Unauthorized" => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };
}
