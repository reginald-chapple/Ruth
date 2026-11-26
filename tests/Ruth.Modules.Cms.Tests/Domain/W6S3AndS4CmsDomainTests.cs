using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Modules.Cms.Domain.Personalization;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Domain;

public sealed class W6S3AndS4CmsDomainTests
{
    [Fact]
    public void AdaptiveMessageResolver_SelectsPriorityAdaptation_AndEnforcesSaturationAndFallback()
    {
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var msg = AdaptiveMessage.Create("Homepage Hero", "homepage-hero").Value;

        var vip = msg.AddAdaptation(
            "VIP Member Banner",
            "Welcome VIP!",
            "Exclusive workshop access",
            order: 0,
            segmentKeys: ["vip-members"],
            viewSaturationCount: 2,
            viewSaturationInDays: 7
        ).Value;

        var fallback = msg.AddAdaptation(
            "General Community Banner",
            "Welcome Everyone",
            "Explore community events",
            order: 10
        ).Value;

        var vipSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "vip-members" };
        var (first, _) = AdaptiveMessageResolver.SelectAdaptation(
            msg,
            vipSegments,
            new Dictionary<Guid, int> { [vip.Id.Value] = 1 },
            now
        );
        Assert.Equal(vip.Id, first?.Id);

        var (afterSaturation, _) = AdaptiveMessageResolver.SelectAdaptation(
            msg,
            vipSegments,
            new Dictionary<Guid, int> { [vip.Id.Value] = 2 },
            now
        );
        Assert.Equal(fallback.Id, afterSaturation?.Id);

        var (nonVip, _) = AdaptiveMessageResolver.SelectAdaptation(
            msg,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<Guid, int>(),
            now
        );
        Assert.Equal(fallback.Id, nonVip?.Id);

        var inactiveMsg = AdaptiveMessage
            .Create("Inactive Hero", "inactive-hero", isActive: false)
            .Value;
        var (noneSelected, _) = AdaptiveMessageResolver.SelectAdaptation(
            inactiveMsg,
            vipSegments,
            new Dictionary<Guid, int>(),
            now
        );
        Assert.Null(noneSelected);
    }

    [Fact]
    public void MobileApplicationAndSignageFeeds_ValidateVersionsDeepLinksAndPlatformOutputs()
    {
        var app = MobileApplication
            .Create(
                "Member Mobile",
                "member-mobile",
                "org.ruth.mobile",
                minimumClientVersion: "2.0.0",
                latestClientVersion: "2.4.0"
            )
            .Value;

        Assert.True(app.IsClientUpgradeRequired("1.9.5"));
        Assert.False(app.IsClientUpgradeRequired("2.1.0"));

        var page = app.AddPage("Events", "events", "/events").Value;
        Assert.True(app.AddDeepLink("/go/events", "https://ruth.local/events", page.Id).IsSuccess);

        var resolved = app.ResolveDeepLink("/go/events/123");
        Assert.True(resolved.Matched);
        Assert.Equal("events", resolved.TargetPageKey);

        var now = DateTimeOffset.UtcNow;
        var signage = SignageApplication
            .Create(
                "Lobby TV",
                "lobby-tv",
                SignagePlatformType.AppleTv,
                slides:
                [
                    new SignageSlideSpec("Active Slide", "Welcome", DurationSeconds: 10, Order: 0),
                    new SignageSlideSpec(
                        "Expired Slide",
                        ExpireDateTimeUtc: now.AddDays(-1),
                        Order: 1
                    ),
                ]
            )
            .Value;

        _ = Assert.Single(signage.GetActiveSlidesAt(now));

        var media = MediaElement
            .Create(
                MediaFolderId.Create(),
                new SyncedMediaElementSpec(
                    "vid-1",
                    "Welcome Video",
                    "Intro",
                    DurationSeconds: 120,
                    ThumbnailUrl: "https://cdn.ruth.local/thumb.jpg",
                    PlaybackUrl: "https://cdn.ruth.local/vid.mp4",
                    HlsManifestUrl: "https://cdn.ruth.local/vid.m3u8",
                    CaptionsUrl: null,
                    Width: 1920,
                    Height: 1080,
                    PublishedDateTimeUtc: now
                )
            )
            .Value;

        var (dsJson, dsCount) = SignageFeedGenerator.GenerateDigitalSignageFeed(
            signage,
            [],
            [media],
            now
        );
        Assert.Equal(2, dsCount);
        Assert.Contains("Active Slide", dsJson, StringComparison.Ordinal);

        var (atvJson, _) = SignageFeedGenerator.GenerateAppleTvFeed(signage, [], [media], now);
        Assert.Contains("apple-tv", atvJson, StringComparison.Ordinal);

        var (rokuJson, rokuCount) = SignageFeedGenerator.GenerateRokuDirectPublisherFeed(
            signage,
            [media],
            now
        );
        Assert.Equal(1, rokuCount);
        Assert.Contains("shortFormVideos", rokuJson, StringComparison.Ordinal);
        Assert.Contains("HLS", rokuJson, StringComparison.Ordinal);
    }
}
