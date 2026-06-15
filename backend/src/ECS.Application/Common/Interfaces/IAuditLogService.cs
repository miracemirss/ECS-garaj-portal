namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Records critical operations (stock decrements, work-order completion,
/// assignment changes) to the audit trail.
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        object? data = null,
        CancellationToken cancellationToken = default);
}
