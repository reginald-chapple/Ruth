using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Slices.Collections;

internal sealed class GetContentCollectionsQueryHandler(SearchDbContext db)
    : IQueryHandler<GetContentCollectionsQuery, IReadOnlyList<ContentCollectionResponse>>
{
    public async Task<Result<IReadOnlyList<ContentCollectionResponse>>> HandleAsync(
        GetContentCollectionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.ContentCollections.AsNoTracking().Include(static c => c.Sources).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static c => c.IsActive);
        }

        var items = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ContentCollectionResponse>>([
            .. items.Select(SearchDtoMapper.ToResponse),
        ]);
    }
}
