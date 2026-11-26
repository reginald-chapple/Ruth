using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Slices.Querying;

internal sealed class ExecuteUniversalSearchQueryHandler(PostgreSqlFullTextSearchEngine engine)
    : IQueryHandler<ExecuteUniversalSearchQuery, SearchResultsResponse>
{
    public async Task<Result<SearchResultsResponse>> HandleAsync(
        ExecuteUniversalSearchQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var results = await engine.SearchAsync(query, cancellationToken);
        return Result.Success(results);
    }
}
