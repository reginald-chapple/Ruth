using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class GetContentChannelFeedQueryHandler(CmsDbContext db, IClock clock)
    : IQueryHandler<GetContentChannelFeedQuery, ContentChannelFeedResponse>
{
    public async Task<Result<ContentChannelFeedResponse>> HandleAsync(
        GetContentChannelFeedQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var slug = CmsTextNormalizer.NormalizeSlug(query.ChannelSlug);
        var channel = await db
            .ContentChannels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
        if (channel is null)
        {
            return Result.Failure<ContentChannelFeedResponse>(
                CmsErrors.NotFound("ContentChannel", query.ChannelSlug)
            );
        }

        if (!channel.EnableRss)
        {
            return Result.Failure<ContentChannelFeedResponse>(
                CmsErrors.Forbidden(
                    "ContentChannel.FeedDisabled",
                    $"Syndication feed is disabled for channel '{channel.Slug}'."
                )
            );
        }

        var now = clock.UtcNow;
        var items = await db
            .ContentChannelItems.AsNoTracking()
            .Where(i => i.ContentChannelId == channel.Id)
            .OrderByDescending(static i => i.StartDateTimeUtc)
            .ToListAsync(cancellationToken);
        List<ContentChannelItem> published = [.. items.Where(i => i.IsPublishedAt(now))];

        var isJson = string.Equals(query.Format, "json", StringComparison.OrdinalIgnoreCase);
        var payload = isJson
            ? ContentChannelFeedGenerator.GenerateJsonFeed11(channel, published, query.BaseOrigin)
            : ContentChannelFeedGenerator.GenerateRss2(channel, published, query.BaseOrigin);

        return Result.Success(
            new ContentChannelFeedResponse(
                channel.Id.Value,
                channel.Slug,
                isJson ? "json" : "rss",
                isJson
                    ? "application/feed+json; charset=utf-8"
                    : "application/rss+xml; charset=utf-8",
                payload,
                published.Count
            )
        );
    }
}
