using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Datasets;

internal sealed class RefreshPersistedDatasetCommandHandler(
    CmsDbContext db,
    PersistedDatasetMaterializer materializer
) : ICommandHandler<RefreshPersistedDatasetCommand, PersistedDatasetResponse>
{
    public async Task<Result<PersistedDatasetResponse>> HandleAsync(
        RefreshPersistedDatasetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var key = CmsTextNormalizer.NormalizeKey(command.AccessKey);
        var dataset = await db.PersistedDatasets.FirstOrDefaultAsync(
            d => d.AccessKey == key,
            cancellationToken
        );
        if (dataset is null)
        {
            return Result.Failure<PersistedDatasetResponse>(
                CmsErrors.NotFound("PersistedDataset", command.AccessKey)
            );
        }

        await materializer.RefreshDatasetAsync(dataset, cancellationToken);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(dataset));
    }
}
