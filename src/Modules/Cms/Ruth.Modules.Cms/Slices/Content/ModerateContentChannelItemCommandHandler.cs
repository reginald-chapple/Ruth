using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Events;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class ModerateContentChannelItemCommandHandler(CmsDbContext db, IClock clock)
    : ICommandHandler<ModerateContentChannelItemCommand, ContentChannelItemResponse>
{
    public async Task<Result<ContentChannelItemResponse>> HandleAsync(
        ModerateContentChannelItemCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var itemId = ContentChannelItemId.From(command.ItemId);
        var item = await db
            .ContentChannelItems.Include(static i => i.ChildAssociations)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<ContentChannelItemResponse>(
                CmsErrors.NotFound("ContentChannelItem", command.ItemId.ToString("D"))
            );
        }

        var now = clock.UtcNow;
        item.Moderate(command.Status, command.ApprovedByPersonId, now);

        if (command.Status == ContentChannelItemStatus.Approved)
        {
            var channelSlug = await db
                .ContentChannels.AsNoTracking()
                .Where(c => c.Id == item.ContentChannelId)
                .Select(static c => c.Slug)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;

            db.EnqueueIntegrationEvent(
                new ContentChannelItemPublishedIntegrationEvent(
                    now,
                    item.ContentChannelId.Value,
                    item.Id.Value,
                    channelSlug,
                    item.Slug,
                    item.Title
                )
            );
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(item));
    }
}
