using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Datasets;

internal sealed class GetPersistedDatasetsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPersistedDatasetsQuery, IReadOnlyList<PersistedDatasetResponse>>
{
    public async Task<Result<IReadOnlyList<PersistedDatasetResponse>>> HandleAsync(
        GetPersistedDatasetsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.PersistedDatasets.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static d => d.IsActive);
        }

        var list = await q.OrderBy(static d => d.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PersistedDatasetResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
