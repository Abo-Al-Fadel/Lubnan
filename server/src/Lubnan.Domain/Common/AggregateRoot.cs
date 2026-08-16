namespace Lubnan.Domain.Common;

/// <summary>
/// The one entity in a cluster that the outside world is allowed to hold a
/// reference to. Everything inside the boundary is reached through it, which
/// is what lets the root be the only place an invariant has to be checked.
/// </summary>
/// <remarks>
/// Aggregate roots are also where domain events are raised. They accumulate on
/// the instance and are drained inside the same transaction that saves the
/// change — see <c>DomainEventInterceptor</c>. An event therefore cannot
/// escape describing a write that was rolled back.
/// </remarks>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
