namespace ECS.Api.Common.Models;

/// <summary>Standard API response envelope (success path may carry no data).</summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public object? Errors { get; init; }
    public int StatusCode { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse Successful(string? message, int statusCode, string? traceId)
        => new() { Success = true, Message = message, StatusCode = statusCode, TraceId = traceId };

    public static ApiResponse Failure(string? message, object? errors, int statusCode, string? traceId)
        => new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode, TraceId = traceId };
}

/// <summary>Standard API response envelope carrying a typed payload.</summary>
public sealed class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Successful(T data, string? message, int statusCode, string? traceId)
        => new() { Success = true, Data = data, Message = message, StatusCode = statusCode, TraceId = traceId };
}
