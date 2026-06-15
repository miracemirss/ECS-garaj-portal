namespace ECS.Domain.Common;

/// <summary>
/// A domain event records something meaningful that happened in the domain
/// (e.g. stock fell below minimum, work order completed). Dispatched after the
/// owning transaction commits. Kept framework-free on purpose.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
