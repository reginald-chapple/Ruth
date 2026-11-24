using Ruth.SharedKernel.Events;

namespace Ruth.SharedKernel.Primitives;

/// <summary>Exposes pending integration events staged on an entity for atomic Outbox persistence.</summary>
public interface IHasIntegrationEvents
{
    /// <summary>Gets the ordered collection of uncommitted integration events.</summary>
    IReadOnlyCollection<IIntegrationEvent> IntegrationEvents { get; }

    /// <summary>Clears staged integration events after Outbox capture.</summary>
    void ClearIntegrationEvents();
}
