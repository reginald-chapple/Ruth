using Ruth.SharedKernel.Events;

namespace Ruth.SharedKernel.Primitives;

/// <summary>Exposes pending domain events recorded on an aggregate root for persistence dispatch.</summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the ordered collection of uncommitted domain events.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears all uncommitted domain events after persistence/dispatch.</summary>
    void ClearDomainEvents();
}
