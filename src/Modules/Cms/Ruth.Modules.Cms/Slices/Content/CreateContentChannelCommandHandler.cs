using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class CreateContentChannelCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateContentChannelCommand, ContentChannelResponse>
{
    public async Task<Result<ContentChannelResponse>> HandleAsync(
        CreateContentChannelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = ContentChannelTypeId.From(command.ContentChannelTypeId);
        var typeExists = await db
            .ContentChannelTypes.AsNoTracking()
            .AnyAsync(t => t.Id == typeId, cancellationToken);
        if (!typeExists)
        {
            return Result.Failure<ContentChannelResponse>(
                CmsErrors.NotFound("ContentChannelType", command.ContentChannelTypeId.ToString("D"))
            );
        }

        var res = ContentChannel.Create(
            typeId,
            command.Name,
            command.Slug,
            command.Description,
            command.IconCssClass,
            command.RequiresApproval,
            command.EnableRss,
            command.ChannelUrl,
            command.ItemUrl,
            command.TimeToLiveMinutes,
            command.ItemsManuallyOrdered,
            command.IsStructuredContent
        );
        if (res.IsFailure)
        {
            return Result.Failure<ContentChannelResponse>(res.PrimaryError);
        }

        var slugExists = await db
            .ContentChannels.AsNoTracking()
            .AnyAsync(c => c.Slug == res.Value.Slug, cancellationToken);
        if (slugExists)
        {
            return Result.Failure<ContentChannelResponse>(
                CmsErrors.Conflict(
                    "ContentChannel.DuplicateSlug",
                    $"ContentChannel slug '{res.Value.Slug}' already exists."
                )
            );
        }

        _ = db.ContentChannels.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }
}
