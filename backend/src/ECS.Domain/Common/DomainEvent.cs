namespace ECS.Domain.Common;

/// <summary>
/// Convenience base record for domain events; stamps the occurrence time.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
