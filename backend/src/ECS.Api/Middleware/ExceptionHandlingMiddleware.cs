using System.Net;
using System.Text.Json;
using ECS.Api.Common.Models;
using ECS.Application.Common.Exceptions;
using ECS.Domain.Exceptions;
using ValidationException = ECS.Application.Common.Exceptions.ValidationException;

namespace ECS.Api.Middleware;

/// <summary>
/// Centralized exception-to-response translation. Produces the standard
/// ApiResponse error envelope (with traceId) so controllers/services never build
/// error responses themselves.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var (status, message, errors) = exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "Validation failed", (object?)ve.Errors),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            DomainException => (HttpStatusCode.Conflict, exception.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred", null)
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception ({TraceId}) on {Path}", traceId, context.Request.Path);
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse.Failure(message, errors, (int)status, traceId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
