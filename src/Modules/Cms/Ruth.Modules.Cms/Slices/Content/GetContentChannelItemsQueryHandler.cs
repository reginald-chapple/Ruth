using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class GetContentChannelItemsQueryHandler(CmsDbContext db, IClock clock)
    : IQueryHandler<GetContentChannelItemsQuery, IReadOnlyList<ContentChannelItemResponse>>
{
    public async Task<Result<IReadOnlyList<ContentChannelItemResponse>>> HandleAsync(
        GetContentChannelItemsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ContentChannelId? channelId = query.ContentChannelId is { } cid
            ? ContentChannelId.From(cid)
            : null;

        if (channelId is null && !string.IsNullOrWhiteSpace(query.ChannelSlug))
        {
            var slug = CmsTextNormalizer.NormalizeSlug(query.ChannelSlug);
            var ch = await db
                .ContentChannels.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
            if (ch is null)
            {
                return Result.Failure<IReadOnlyList<ContentChannelItemResponse>>(
                    CmsErrors.NotFound("ContentChannel", query.ChannelSlug)
                );
            }

            channelId = ch.Id;
        }

        var q = db
            .ContentChannelItems.AsNoTracking()
            .Include(static i => i.ChildAssociations)
            .AsQueryable();

        if (channelId is { } resolvedId)
        {
            q = q.Where(i => i.ContentChannelId == resolvedId);
        }

        var items = await q.OrderByDescending(static i => i.Priority)
            .ThenByDescending(static i => i.StartDateTimeUtc)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        var filtered = items.Where(i =>
            (!query.PublishedOnly || i.IsPublishedAt(now))
            && (query.TopicId is null || i.TopicIds.Contains(query.TopicId.Value))
        );

        return Result.Success<IReadOnlyList<ContentChannelItemResponse>>([
            .. filtered.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
