using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Reporting.Slices.Events;

internal sealed class ReportingPersonMergedConsumer(ReportingDbContext db)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;

        var ownedTemplates = await db
            .MergeTemplates.Where(t => t.OwnerPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var tpl in ownedTemplates)
        {
            tpl.ReassignOwner(survivingId);
        }

        var persistedRows = await db
            .DataViewPersistedValues.Where(v => v.EntityId == retiredId)
            .ToListAsync(cancellationToken);
        if (persistedRows.Count > 0)
        {
            var dataViewIds = persistedRows.Select(static r => r.DataViewId).Distinct().ToList();
            var existingSurviving = await db
                .DataViewPersistedValues.AsNoTracking()
                .Where(v => dataViewIds.Contains(v.DataViewId) && v.EntityId == survivingId)
                .Select(static v => v.DataViewId)
                .ToListAsync(cancellationToken);
            var existingSet = existingSurviving.ToHashSet();

            foreach (var row in persistedRows)
            {
                _ = db.DataViewPersistedValues.Remove(row);
                if (!existingSet.Contains(row.DataViewId))
                {
                    _ = db.DataViewPersistedValues.Add(
                        new DataViewPersistedValue(
                            row.DataViewId,
                            survivingId,
                            row.PersistedDateTimeUtc
                        )
                    );
                }
            }
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
