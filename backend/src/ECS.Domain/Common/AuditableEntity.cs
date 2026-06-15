namespace ECS.Domain.Common;

/// <summary>
/// Entity that carries audit metadata. Populated automatically by the
/// persistence-layer AuditableEntityInterceptor on SaveChanges.
/// Soft-delete fields are included so critical records are never hard-deleted.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
