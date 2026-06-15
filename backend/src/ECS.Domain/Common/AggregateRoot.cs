namespace ECS.Domain.Common;

/// <summary>
/// Base class for aggregate roots: auditable, identifiable, and able to raise
/// domain events that the infrastructure dispatches after commit.
/// </summary>
public abstract class AggregateRoot : AuditableEntity, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
