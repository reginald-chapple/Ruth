using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Cms.Slices.Events;

internal sealed class CmsPersonMergedConsumer(CmsDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;

        var htmlContents = await dbContext
            .HtmlContents.Where(h => h.ApprovedByPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var h in htmlContents)
        {
            _ = h.RepointApprovedByPerson(retiredId, survivingId);
        }

        var items = await dbContext
            .ContentChannelItems.Where(i =>
                i.AuthorPersonId == retiredId || i.ApprovedByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            _ = item.RepointPerson(retiredId, survivingId);
        }

        var sections = await dbContext
            .PersonalLinkSections.Where(s => s.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var sec in sections)
        {
            _ = sec.RepointPerson(retiredId, survivingId);
        }

        var sessions = await dbContext
            .InteractionSessions.Where(s => s.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            _ = session.RepointPerson(retiredId, survivingId);
        }

        var interactions = await dbContext
            .Interactions.Where(i => i.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var interaction in interactions)
        {
            _ = interaction.RepointPerson(retiredId, survivingId);
        }

        await RepointSegmentMembershipsAsync(retiredId, survivingId, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RepointSegmentMembershipsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken ct
    )
    {
        var retiredMemberships = await dbContext
            .PersonalizationSegmentMemberships.Where(m => m.PersonId == retiredId)
            .ToListAsync(ct);

        if (retiredMemberships.Count == 0)
        {
            return;
        }

        var existingSurvivingSegments = await dbContext
            .PersonalizationSegmentMemberships.Where(m => m.PersonId == survivingId)
            .Select(static m => m.SegmentId)
            .ToListAsync(ct);
        var survivingSet = new HashSet<Domain.PersonalizationSegmentId>(existingSurvivingSegments);

        foreach (var membership in retiredMemberships)
        {
            _ = dbContext.PersonalizationSegmentMemberships.Remove(membership);
            if (!survivingSet.Contains(membership.SegmentId))
            {
                _ = dbContext.PersonalizationSegmentMemberships.Add(
                    new PersonalizationSegmentMembership(
                        membership.SegmentId,
                        survivingId,
                        membership.AddedDateTimeUtc,
                        membership.AffinityScore
                    )
                );
                _ = survivingSet.Add(membership.SegmentId);
            }
        }
    }
}
