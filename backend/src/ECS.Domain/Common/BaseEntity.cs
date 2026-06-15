namespace ECS.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides a surrogate identity.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
