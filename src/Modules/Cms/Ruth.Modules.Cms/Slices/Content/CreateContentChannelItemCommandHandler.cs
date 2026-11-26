using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Events;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class CreateContentChannelItemCommandHandler(CmsDbContext db, IClock clock)
    : ICommandHandler<CreateContentChannelItemCommand, ContentChannelItemResponse>
{
    public async Task<Result<ContentChannelItemResponse>> HandleAsync(
        CreateContentChannelItemCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var channelId = ContentChannelId.From(command.ContentChannelId);
        var channel = await db
            .ContentChannels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel is null)
        {
            return Result.Failure<ContentChannelItemResponse>(
                CmsErrors.NotFound("ContentChannel", command.ContentChannelId.ToString("D"))
            );
        }

        var now = clock.UtcNow;
        var res = BuildItem(channel, command, now);
        if (res.IsFailure)
        {
            return Result.Failure<ContentChannelItemResponse>(res.PrimaryError);
        }

        var slugExists = await db
            .ContentChannelItems.AsNoTracking()
            .AnyAsync(
                i => i.ContentChannelId == channelId && i.Slug == res.Value.Slug,
                cancellationToken
            );
        if (slugExists)
        {
            return Result.Failure<ContentChannelItemResponse>(
                CmsErrors.Conflict(
                    "ContentChannelItem.DuplicateSlug",
                    $"Item slug '{res.Value.Slug}' already exists in channel '{channel.Slug}'."
                )
            );
        }

        _ = db.ContentChannelItems.Add(res.Value);
        StagePublishedEventIfApproved(channel, res.Value, now);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }

    private static Result<ContentChannelItem> BuildItem(
        ContentChannel channel,
        CreateContentChannelItemCommand command,
        DateTimeOffset now
    )
    {
        return ContentChannelItem.Create(
            channel.Id,
            command.Title,
            command.Slug,
            command.Content,
            channel.RequiresApproval,
            command.StartDateTimeUtc ?? now,
            command.ExpireDateTimeUtc,
            command.Summary,
            command.Priority,
            command.Order,
            command.AuthorPersonId,
            command.PrimaryTopicId is { } tid ? ContentTopicId.From(tid) : null,
            command.TopicIds,
            command.AttributeValues,
            now
        );
    }

    private void StagePublishedEventIfApproved(
        ContentChannel channel,
        ContentChannelItem item,
        DateTimeOffset now
    )
    {
        if (item.Status == ContentChannelItemStatus.Approved)
        {
            db.EnqueueIntegrationEvent(
                new ContentChannelItemPublishedIntegrationEvent(
                    now,
                    channel.Id.Value,
                    item.Id.Value,
                    channel.Slug,
                    item.Slug,
                    item.Title
                )
            );
        }
    }
}
