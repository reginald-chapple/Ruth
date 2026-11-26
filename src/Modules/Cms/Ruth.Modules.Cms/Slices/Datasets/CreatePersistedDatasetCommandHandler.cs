using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Datasets;

internal sealed class CreatePersistedDatasetCommandHandler(
    CmsDbContext db,
    PersistedDatasetMaterializer materializer
) : ICommandHandler<CreatePersistedDatasetCommand, PersistedDatasetResponse>
{
    public async Task<Result<PersistedDatasetResponse>> HandleAsync(
        CreatePersistedDatasetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = PersistedDataset.Create(
            command.AccessKey,
            command.Name,
            command.DataSourceKind,
            command.Description,
            command.SourceConfiguration,
            command.RefreshIntervalMinutes,
            command.ExpireDateTimeUtc,
            command.MemoryCacheDurationMinutes,
            command.AllowManualRefresh
        );
        if (res.IsFailure)
        {
            return Result.Failure<PersistedDatasetResponse>(res.PrimaryError);
        }

        var exists = await db
            .PersistedDatasets.AsNoTracking()
            .AnyAsync(d => d.AccessKey == res.Value.AccessKey, cancellationToken);
        if (exists)
        {
            return Result.Failure<PersistedDatasetResponse>(
                CmsErrors.Conflict(
                    "PersistedDataset.DuplicateKey",
                    $"PersistedDataset access key '{res.Value.AccessKey}' already exists."
                )
            );
        }

        if (command.RefreshImmediately)
        {
            await materializer.RefreshDatasetAsync(res.Value, cancellationToken);
        }

        _ = db.PersistedDatasets.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }
}
