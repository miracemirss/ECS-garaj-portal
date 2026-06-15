namespace ECS.Domain.Entities;

/// <summary>
/// Append-only audit record (old/new JSONB snapshots). Written by the DB audit
/// trigger and/or IAuditLogService. Uses a numeric identity (high volume), so it
/// does not derive from the Guid-keyed BaseEntity.
/// </summary>
public class AuditLog
{
    private AuditLog() { }

    public long Id { get; private set; }
    public string TableName { get; private set; } = null!;
    public Guid? RecordId { get; private set; }
    public string Action { get; private set; } = null!;   // INSERT | UPDATE | DELETE
    public string? OldData { get; private set; }          // JSONB (serialized)
    public string? NewData { get; private set; }          // JSONB (serialized)
    public string[]? ChangedColumns { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? ChangedByName { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? ClientIp { get; private set; }

    public static AuditLog Create(
        string tableName, Guid? recordId, string action,
        string? oldData, string? newData,
        Guid? changedBy, string? changedByName, DateTime utcNow,
        string[]? changedColumns = null, string? clientIp = null)
        => new()
        {
            TableName = tableName,
            RecordId = recordId,
            Action = action,
            OldData = oldData,
            NewData = newData,
            ChangedColumns = changedColumns,
            ChangedBy = changedBy,
            ChangedByName = changedByName,
            ChangedAt = utcNow,
            ClientIp = clientIp
        };
}
