using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Search.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Search.Slices.Events;

internal sealed class SearchPersonMergedConsumer(SearchDbContext db)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var retiredDocs = await db
            .SearchDocuments.Where(d =>
                d.EntityTypeKey == "Ruth.Crm.Person"
                && d.EntityId == integrationEvent.RetiredPersonId
            )
            .ToListAsync(cancellationToken);
        if (retiredDocs.Count == 0)
        {
            return;
        }

        db.SearchDocuments.RemoveRange(retiredDocs);
        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
