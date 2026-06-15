namespace ECS.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots — the only entities a repository may
/// load and persist directly. Enforces aggregate boundaries.
/// </summary>
public interface IAggregateRoot
{
}
