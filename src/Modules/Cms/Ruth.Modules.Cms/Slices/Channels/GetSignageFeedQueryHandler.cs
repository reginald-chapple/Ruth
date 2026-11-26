using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class GetSignageFeedQueryHandler(CmsDbContext db, IClock clock)
    : IQueryHandler<GetSignageFeedQuery, SignageFeedResponse>
{
    public async Task<Result<SignageFeedResponse>> HandleAsync(
        GetSignageFeedQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var slug = CmsTextNormalizer.NormalizeSlug(query.AppSlug);
        var app = await db
            .SignageApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive, cancellationToken);
        if (app is null)
        {
            return Result.Failure<SignageFeedResponse>(
                CmsErrors.NotFound("SignageApplication", query.AppSlug)
            );
        }

        var now = clock.UtcNow;
        var channelItems = await LoadPublishedChannelItemsAsync(
            app.DefaultContentChannelId,
            now,
            cancellationToken
        );
        var mediaElements = await LoadMediaElementsAsync(app.MediaFolderId, cancellationToken);

        var fmt = query.Format.Trim().ToLowerInvariant();
        var (json, count) = fmt switch
        {
            "apple-tv" => SignageFeedGenerator.GenerateAppleTvFeed(
                app,
                channelItems,
                mediaElements,
                now
            ),
            "roku" => SignageFeedGenerator.GenerateRokuDirectPublisherFeed(app, mediaElements, now),
            _ => SignageFeedGenerator.GenerateDigitalSignageFeed(
                app,
                channelItems,
                mediaElements,
                now
            ),
        };

        return Result.Success(
            new SignageFeedResponse(
                app.Id.Value,
                app.Slug,
                app.Platform,
                fmt,
                "application/json; charset=utf-8",
                app.RefreshIntervalSeconds,
                json,
                count,
                now
            )
        );
    }

    private async Task<List<ContentChannelItem>> LoadPublishedChannelItemsAsync(
        ContentChannelId? channelId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        if (channelId is not { } cid)
        {
            return [];
        }

        var raw = await db
            .ContentChannelItems.AsNoTracking()
            .Where(i => i.ContentChannelId == cid)
            .OrderByDescending(static i => i.StartDateTimeUtc)
            .Take(20)
            .ToListAsync(ct);
        return [.. raw.Where(i => i.IsPublishedAt(now))];
    }

    private async Task<List<MediaElement>> LoadMediaElementsAsync(
        MediaFolderId? folderId,
        CancellationToken ct
    )
    {
        return folderId is not { } fid
            ? []
            : await db
                .MediaElements.AsNoTracking()
                .Where(e => e.MediaFolderId == fid)
                .OrderByDescending(static e => e.PublishedDateTimeUtc)
                .Take(30)
                .ToListAsync(ct);
    }
}
