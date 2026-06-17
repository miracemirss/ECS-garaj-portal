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
            ValidationException ve => (HttpStatusCode.BadRequest, "Doğrulama hatası.", (object?)ve.Errors),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            DomainException => (HttpStatusCode.Conflict, exception.Message, null),
            _ => TranslateDatabaseError(exception)
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

    /// <summary>
    /// Veritabanı kısıtı ihlallerini (PostgreSQL) anlamlı HTTP durum kodlarına çevirir.
    /// Böylece benzersizlik/CHECK/foreign-key ihlalleri kullanıcıya 500 yerine 409/400
    /// olarak Türkçe mesajla döner. PostgreSQL'e doğrudan tip bağımlılığı eklememek için
    /// inner exception'ın SqlState değeri reflection ile okunur.
    /// </summary>
    private static (HttpStatusCode, string, object?) TranslateDatabaseError(Exception exception)
    {
        var sqlState = ExtractSqlState(exception);
        return sqlState switch
        {
            "23505" => (HttpStatusCode.Conflict, "Bu kayıt zaten mevcut. Benzersizlik kuralı ihlal edildi.", null),
            "23503" => (HttpStatusCode.BadRequest, "İlişkili kayıt bulunamadı veya silinemiyor.", null),
            "23514" => (HttpStatusCode.BadRequest, "Gönderilen veriler geçerli değil.", null),
            _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.", null)
        };
    }

    private static string? ExtractSqlState(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().Name == "PostgresException")
            {
                return current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            }
        }
        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
