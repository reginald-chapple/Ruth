using Microsoft.EntityFrameworkCore;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.EntityFramework;

internal static class OutboxEventStager
{
    public static void CaptureIntegrationEvents(
        ModuleDbContext moduleContext,
        StagedSaveChangesState state
    )
    {
        var entities = moduleContext
            .ChangeTracker.Entries<IHasIntegrationEvents>()
            .Select(static entry => entry.Entity)
            .Where(static entity => entity.IntegrationEvents.Count > 0)
            .ToArray();

        foreach (var entity in entities)
        {
            state.IntegrationEventSources.Add(entity);
            foreach (var integrationEvent in entity.IntegrationEvents)
            {
                state.StagedOutboxMessages.Add(
                    moduleContext.StageIntegrationEvent(integrationEvent)
                );
            }
        }
    }

    public static void StageDomainEvents(
        DbContext context,
        ModuleDbContext? moduleContext,
        StagedSaveChangesState state
    )
    {
        var aggregates = context
            .ChangeTracker.Entries<IHasDomainEvents>()
            .Select(static entry => entry.Entity)
            .Where(static aggregate => aggregate.DomainEvents.Count > 0)
            .ToArray();

        foreach (var aggregate in aggregates)
        {
            var snapshot = aggregate.DomainEvents.ToArray();
            state.DomainEvents.Add(new StagedDomainEvents(aggregate, snapshot));
            EnqueueDualIntegrationEvents(moduleContext, snapshot, state);
        }
    }

    private static void EnqueueDualIntegrationEvents(
        ModuleDbContext? moduleContext,
        IDomainEvent[] snapshot,
        StagedSaveChangesState state
    )
    {
        if (moduleContext is null)
        {
            return;
        }

        foreach (var domainEvent in snapshot)
        {
            if (domainEvent is IIntegrationEvent integrationEvent)
            {
                state.StagedOutboxMessages.Add(
                    moduleContext.StageIntegrationEvent(integrationEvent)
                );
            }
        }
    }
}
