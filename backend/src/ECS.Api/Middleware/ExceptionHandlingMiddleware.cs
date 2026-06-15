using System.Net;
using System.Text.Json;
using ECS.Application.Common.Exceptions;
using ECS.Domain.Exceptions;

namespace ECS.Api.Middleware;

/// <summary>
/// Centralized exception-to-HTTP translation so controllers and services never
/// build error responses themselves.
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
        var (status, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            DomainException => (HttpStatusCode.Conflict, "Business rule violation"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            title,
            status = (int)status,
            detail = exception.Message
        });

        await context.Response.WriteAsync(payload);
    }
}
