namespace ECS.Domain.Interfaces;

/// <summary>Contract for entities that support soft delete instead of physical removal.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid? DeletedBy { get; }

    void MarkDeleted(Guid? deletedBy, DateTime utcNow);
    void Restore();
}
