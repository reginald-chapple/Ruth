using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Events.Slices.Events;

internal sealed class EventsPersonMergedConsumer(EventsDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;
        if (retiredId == Guid.Empty || survivingId == Guid.Empty || retiredId == survivingId)
        {
            return;
        }

        var changed = false;
        changed |= await RepointEventItemsAsync(retiredId, survivingId, cancellationToken);
        changed |= await RepointInteractiveSessionsAsync(retiredId, survivingId, cancellationToken);
        changed |= await RepointRegistrationsAsync(retiredId, survivingId, cancellationToken);

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RepointEventItemsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;
        var items = await dbContext
            .EventItems.Include(static i => i.Occurrences)
            .Where(i =>
                i.ApprovedByPersonId == retiredId
                || i.Occurrences.Any(o => o.ContactPersonId == retiredId)
            )
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            modified |= item.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointInteractiveSessionsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;
        var sessions = await dbContext
            .InteractiveSessions.Include(static s => s.Responses)
            .Where(s =>
                s.ModeratorPersonId == retiredId
                || s.Responses.Any(r => r.ResponderPersonId == retiredId)
            )
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            modified |= session.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointRegistrationsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;
        var registrations = await dbContext
            .Registrations.Include(static r => r.Registrants)
            .Where(r =>
                r.RegistrarPersonId == retiredId
                || r.Registrants.Any(reg => reg.PersonId == retiredId)
            )
            .ToListAsync(cancellationToken);
        foreach (var reg in registrations)
        {
            modified |= reg.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }
}
