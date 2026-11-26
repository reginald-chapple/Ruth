using System.Text.Json;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Media;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Clean-room JSON feed generator for Digital Signage, Apple TV, and Roku Direct Publisher displays.</summary>
public static class SignageFeedGenerator
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static (string Json, int ItemCount) GenerateDigitalSignageFeed(
        SignageApplication app,
        IReadOnlyList<ContentChannelItem> channelItems,
        IReadOnlyList<MediaElement> mediaElements,
        DateTimeOffset nowUtc
    )
    {
        var activeSlides = app.GetActiveSlidesAt(nowUtc);
        var payload = new
        {
            contractVersion = "1.0",
            platform = "digital-signage",
            application = new
            {
                id = app.Id.Value,
                app.Name,
                app.Slug,
                app.RefreshIntervalSeconds,
            },
            generatedAtUtc = nowUtc,
            slides = activeSlides,
            announcements = channelItems.Select(static i => new
            {
                id = i.Id.Value,
                i.Title,
                i.Slug,
                i.Summary,
                i.StartDateTimeUtc,
            }),
            media = mediaElements.Select(static m => new
            {
                id = m.Id.Value,
                m.Name,
                m.DurationSeconds,
                m.ThumbnailUrl,
                m.PlaybackUrl,
                m.HlsManifestUrl,
            }),
        };

        return (
            JsonSerializer.Serialize(payload, _jsonOptions),
            activeSlides.Count + channelItems.Count + mediaElements.Count
        );
    }

    public static (string Json, int ItemCount) GenerateAppleTvFeed(
        SignageApplication app,
        IReadOnlyList<ContentChannelItem> channelItems,
        IReadOnlyList<MediaElement> mediaElements,
        DateTimeOffset nowUtc
    )
    {
        var activeSlides = app.GetActiveSlidesAt(nowUtc);
        var payload = new
        {
            contractVersion = "1.0",
            platform = "apple-tv",
            appTitle = app.Name,
            refreshIntervalSeconds = app.RefreshIntervalSeconds,
            generatedAtUtc = nowUtc,
            heroBanners = activeSlides.Select(static s => new
            {
                s.Title,
                s.Subtitle,
                imageUrl = s.ImageUrl,
                videoUrl = s.VideoUrl,
                durationSeconds = s.DurationSeconds,
            }),
            shelves = new object[]
            {
                new
                {
                    title = "Featured Videos",
                    items = mediaElements.Select(static m => new
                    {
                        id = m.Id.Value.ToString("D"),
                        title = m.Name,
                        description = m.Description,
                        durationSeconds = m.DurationSeconds,
                        artworkUrl = m.ThumbnailUrl,
                        streamUrl = m.HlsManifestUrl ?? m.PlaybackUrl,
                    }),
                },
                new
                {
                    title = "Updates & Highlights",
                    items = channelItems.Select(static i => new
                    {
                        id = i.Id.Value.ToString("D"),
                        title = i.Title,
                        summary = i.Summary,
                        publishedAtUtc = i.StartDateTimeUtc,
                    }),
                },
            },
        };

        return (
            JsonSerializer.Serialize(payload, _jsonOptions),
            activeSlides.Count + mediaElements.Count + channelItems.Count
        );
    }

    public static (string Json, int ItemCount) GenerateRokuDirectPublisherFeed(
        SignageApplication app,
        IReadOnlyList<MediaElement> mediaElements,
        DateTimeOffset nowUtc
    )
    {
        var shortFormVideos = mediaElements
            .Select(static m => new
            {
                id = m.Id.Value.ToString("D"),
                title = m.Name,
                shortDescription = m.Description ?? m.Name,
                thumbnail = m.ThumbnailUrl ?? "https://ruth.local/assets/video-placeholder.png",
                releaseDate = (m.PublishedDateTimeUtc ?? m.CreatedOnUtc)
                    .ToUniversalTime()
                    .ToString("O"),
                content = new
                {
                    dateAdded = m.CreatedOnUtc.ToUniversalTime().ToString("O"),
                    duration = Math.Max(1, m.DurationSeconds),
                    videos = new[]
                    {
                        new
                        {
                            url = m.HlsManifestUrl ?? m.PlaybackUrl ?? string.Empty,
                            quality = "HD",
                            videoType = m.HlsManifestUrl is not null ? "HLS" : "MP4",
                        },
                    },
                },
            })
            .ToList();

        var payload = new
        {
            providerName = app.Name,
            lastUpdated = nowUtc.ToUniversalTime().ToString("O"),
            language = "en-US",
            shortFormVideos,
            playlists = new[]
            {
                new
                {
                    name = app.Name,
                    itemIds = shortFormVideos.Select(static v => v.id).ToList(),
                },
            },
        };

        return (JsonSerializer.Serialize(payload, _jsonOptions), shortFormVideos.Count);
    }
}
