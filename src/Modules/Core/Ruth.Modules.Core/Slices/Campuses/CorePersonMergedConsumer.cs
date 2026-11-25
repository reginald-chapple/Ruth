using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class CorePersonMergedConsumer(CoreDbContext dbContext, ICoreReadCache readCache)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var campuses = await dbContext
            .Campuses.Where(c => c.LeaderPersonId == integrationEvent.RetiredPersonId)
            .ToListAsync(cancellationToken);

        if (campuses.Count == 0)
        {
            return;
        }

        foreach (var campus in campuses)
        {
            campus.RepointLeaderPerson(integrationEvent.SurvivingPersonId);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await readCache.InvalidateAllAsync(cancellationToken);
    }
}
