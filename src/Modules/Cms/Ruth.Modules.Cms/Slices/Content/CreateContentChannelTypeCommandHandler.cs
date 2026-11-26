using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class CreateContentChannelTypeCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateContentChannelTypeCommand, ContentChannelTypeResponse>
{
    public async Task<Result<ContentChannelTypeResponse>> HandleAsync(
        CreateContentChannelTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = ContentChannelType.Create(
            command.Name,
            command.Key,
            command.DateRangeType,
            command.DisablePriority,
            command.DisableStatus,
            command.IncludeTime
        );
        if (res.IsFailure)
        {
            return Result.Failure<ContentChannelTypeResponse>(res.PrimaryError);
        }

        var exists = await db
            .ContentChannelTypes.AsNoTracking()
            .AnyAsync(t => t.Key == res.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<ContentChannelTypeResponse>(
                CmsErrors.Conflict(
                    "ContentChannelType.DuplicateKey",
                    $"ContentChannelType '{res.Value.Key}' already exists."
                )
            );
        }

        _ = db.ContentChannelTypes.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }
}
