using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.EntityFramework;

internal sealed class StagedSaveChangesState
{
    public List<StagedDomainEvents> DomainEvents { get; } = [];
    public List<IHasIntegrationEvents> IntegrationEventSources { get; } = [];
    public List<OutboxMessage> StagedOutboxMessages { get; } = [];

    public bool HasAny =>
        DomainEvents.Count > 0
        || IntegrationEventSources.Count > 0
        || StagedOutboxMessages.Count > 0;
}
