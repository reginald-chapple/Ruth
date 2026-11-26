using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Datasets;

internal sealed class GetPersistedDatasetByKeyQueryHandler(
    CmsDbContext db,
    PersistedDatasetMaterializer materializer,
    IClock clock
) : IQueryHandler<GetPersistedDatasetByKeyQuery, PersistedDatasetResponse>
{
    public async Task<Result<PersistedDatasetResponse>> HandleAsync(
        GetPersistedDatasetByKeyQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var key = CmsTextNormalizer.NormalizeKey(query.AccessKey);
        var dataset = await db.PersistedDatasets.FirstOrDefaultAsync(
            d => d.AccessKey == key && d.IsActive,
            cancellationToken
        );
        if (dataset is null)
        {
            return Result.Failure<PersistedDatasetResponse>(
                CmsErrors.NotFound("PersistedDataset", query.AccessKey)
            );
        }

        if (query.RefreshIfStale && dataset.IsStaleAt(clock.UtcNow))
        {
            await materializer.RefreshDatasetAsync(dataset, cancellationToken);
            _ = await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(CmsContentProjectionMapper.ToResponse(dataset));
    }
}
