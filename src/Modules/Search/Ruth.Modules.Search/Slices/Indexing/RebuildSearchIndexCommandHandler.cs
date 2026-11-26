using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Infrastructure;
using Ruth.Modules.Search.Infrastructure.Indexing;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Search.Slices.Indexing;

internal sealed class RebuildSearchIndexCommandHandler(
    SearchDbContext db,
    IEnumerable<ISearchDocumentProvider> providers,
    SearchDocumentUpsertService upsertService,
    IClock clock
) : ICommandHandler<RebuildSearchIndexCommand, SearchIndexRebuildResponse>
{
    public async Task<Result<SearchIndexRebuildResponse>> HandleAsync(
        RebuildSearchIndexCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sw = Stopwatch.StartNew();
        var providerMap = providers.ToDictionary(
            static p => p.ProviderKey,
            StringComparer.OrdinalIgnoreCase
        );

        if (command.ContentCollectionId is { } rawCollId && rawCollId != Guid.Empty)
        {
            return await RebuildCollectionAsync(
                ContentCollectionId.From(rawCollId),
                providerMap,
                sw,
                cancellationToken
            );
        }

        var selected =
            string.IsNullOrWhiteSpace(command.ProviderKey) ? [.. providerMap.Values]
            : providerMap.TryGetValue(command.ProviderKey.Trim(), out var single)
                ? new List<ISearchDocumentProvider> { single }
            : [];

        var totalUpserted = 0;
        var totalSkipped = 0;
        foreach (var provider in selected)
        {
            var docs = await provider.LoadDocumentsAsync(
                new SearchProviderLoadRequest(MaxDocuments: 250),
                cancellationToken
            );
            var (up, sk) = await upsertService.UpsertBatchAsync(docs, cancellationToken);
            totalUpserted += up;
            totalSkipped += sk;
        }

        sw.Stop();
        return Result.Success(
            new SearchIndexRebuildResponse(
                selected.Count,
                totalUpserted,
                totalSkipped,
                (int)sw.ElapsedMilliseconds
            )
        );
    }

    private async Task<Result<SearchIndexRebuildResponse>> RebuildCollectionAsync(
        ContentCollectionId collectionId,
        Dictionary<string, ISearchDocumentProvider> providerMap,
        Stopwatch sw,
        CancellationToken ct
    )
    {
        var collection = await db
            .ContentCollections.Include(static c => c.Sources)
            .FirstOrDefaultAsync(c => c.Id == collectionId, ct);
        if (collection is null)
        {
            return Result.Failure<SearchIndexRebuildResponse>(
                SearchErrors.NotFound("Collection.NotFound", "ContentCollection was not found.")
            );
        }

        var executed = 0;
        var totalUpserted = 0;
        var totalSkipped = 0;
        foreach (var src in collection.Sources.Where(static s => s.IsActive))
        {
            if (!providerMap.TryGetValue(src.ProviderKey, out var provider))
            {
                continue;
            }

            var docs = await provider.LoadDocumentsAsync(
                new SearchProviderLoadRequest(
                    src.OccurrencesToShow,
                    collection.Id.Value,
                    src.FilterConfigJson
                ),
                ct
            );
            var (up, sk) = await upsertService.UpsertBatchAsync(docs, ct);
            executed++;
            totalUpserted += up;
            totalSkipped += sk;
        }

        collection.RecordIndexed(totalUpserted + totalSkipped, clock.UtcNow);
        _ = await db.SaveChangesAsync(ct);
        sw.Stop();
        return Result.Success(
            new SearchIndexRebuildResponse(
                executed,
                totalUpserted,
                totalSkipped,
                (int)sw.ElapsedMilliseconds
            )
        );
    }
}
