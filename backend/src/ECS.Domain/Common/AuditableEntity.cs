using ECS.Domain.Interfaces;

namespace ECS.Domain.Common;

/// <summary>
/// Entity carrying audit metadata and soft-delete state. The audit timestamp/user
/// fields are populated automatically by the persistence-layer
/// AuditableEntityInterceptor on SaveChanges. All timestamps are UTC.
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>Marks the entity as soft-deleted. Critical records are never hard-deleted.</summary>
    public void MarkDeleted(Guid? deletedBy, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAt = utcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
