using System.Diagnostics;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Infrastructure.Indexing;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Slices.Indexing;

internal sealed class UpsertSearchDocumentsCommandHandler(SearchDocumentUpsertService upsertService)
    : ICommandHandler<UpsertSearchDocumentsCommand, SearchIndexRebuildResponse>
{
    public async Task<Result<SearchIndexRebuildResponse>> HandleAsync(
        UpsertSearchDocumentsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sw = Stopwatch.StartNew();
        var (upserted, skipped) = await upsertService.UpsertBatchAsync(
            command.Documents,
            cancellationToken
        );
        sw.Stop();
        return Result.Success(
            new SearchIndexRebuildResponse(0, upserted, skipped, (int)sw.ElapsedMilliseconds)
        );
    }
}
