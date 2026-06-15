using System.Text.Json;
using ECS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECS.Infrastructure.Services;

/// <summary>
/// Records high-level business audit events through the logging pipeline (Serilog).
/// Row-level before/after data is captured separately by the database audit trigger.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly ICurrentUserService _currentUser;

    public AuditLogService(ILogger<AuditLogService> logger, ICurrentUserService currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public Task LogAsync(string action, string entityName, string? entityId, object? data = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AUDIT {Action} {Entity} {EntityId} by {UserId} {Data}",
            action, entityName, entityId, _currentUser.UserId,
            data is null ? null : JsonSerializer.Serialize(data));

        return Task.CompletedTask;
    }
}
