using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Indexing;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Search.Infrastructure.Indexing;

internal sealed class SearchDocumentUpsertService(SearchDbContext db, IClock clock)
{
    public async Task<(int Upserted, int Skipped)> UpsertBatchAsync(
        IReadOnlyList<SearchDocumentUpsertSpec> specs,
        CancellationToken ct
    )
    {
        if (specs.Count == 0)
        {
            return (0, 0);
        }

        var nowUtc = clock.UtcNow;
        var upserted = 0;
        var skipped = 0;

        foreach (var spec in specs)
        {
            ContentCollectionId? collId =
                spec.ContentCollectionId is { } cid && cid != Guid.Empty
                    ? ContentCollectionId.From(cid)
                    : null;
            var entityType = spec.EntityTypeKey.Trim();
            var existing = await db.SearchDocuments.FirstOrDefaultAsync(
                d =>
                    d.ContentCollectionId == collId
                    && d.EntityTypeKey == entityType
                    && d.EntityId == spec.EntityId,
                ct
            );

            if (existing is not null)
            {
                if (existing.ApplyUpdate(spec, nowUtc))
                {
                    upserted++;
                }
                else
                {
                    skipped++;
                }

                continue;
            }

            var created = SearchDocument.Create(spec, nowUtc);
            if (created.IsSuccess)
            {
                _ = db.SearchDocuments.Add(created.Value);
                upserted++;
            }
        }

        if (upserted > 0)
        {
            _ = await db.SaveChangesAsync(ct);
        }

        return (upserted, skipped);
    }
}
