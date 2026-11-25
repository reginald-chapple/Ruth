using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class CorePersonMergedConsumer(CoreDbContext dbContext, ICoreReadCache readCache)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var branches = await dbContext
            .Branches.Where(b => b.LeaderPersonId == integrationEvent.RetiredPersonId)
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            return;
        }

        foreach (var branch in branches)
        {
            branch.RepointLeaderPerson(integrationEvent.SurvivingPersonId);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await readCache.InvalidateAllAsync(cancellationToken);
    }
}
