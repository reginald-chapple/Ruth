using Ruth.SharedKernel.Events;

namespace Ruth.SharedKernel.Primitives;

/// <summary>Base type for DDD aggregate roots that enforce invariants and emit <see cref="IDomainEvent" />s.</summary>
/// <typeparam name="TId">The strongly-typed aggregate identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Initializes an aggregate root for persistence materialization.</summary>
    protected AggregateRoot() { }

    /// <summary>Initializes an aggregate root with a strongly-typed identifier.</summary>
    /// <param name="id">The aggregate identifier.</param>
    protected AggregateRoot(TId id)
        : base(id) { }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>Records a domain event to be dispatched when the aggregate is persisted.</summary>
    /// <param name="domainEvent">The domain event raised by a state transition.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
