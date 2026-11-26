using System.Text.Json;
using System.Xml.Linq;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Bookmarks;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Modules.Cms.Domain.Media;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Domain;

public sealed class W6S2CmsContentAndMediaDomainTests
{
    [Fact]
    public void ContentChannelItem_SchedulingModerationAndFeeds_WorkDeterministically()
    {
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var type = ContentChannelType.Create("Articles", "articles").Value;
        var channel = ContentChannel
            .Create(type.Id, "Community News", "community-news", requiresApproval: true)
            .Value;

        var item = ContentChannelItem
            .Create(
                channel.Id,
                "Annual Summit",
                "annual-summit",
                "<p>Join us for the summit.</p>",
                requiresApproval: true,
                startDateTimeUtc: now.AddHours(-1),
                expireDateTimeUtc: now.AddDays(5),
                summary: "Summit highlights",
                nowUtc: now
            )
            .Value;

        Assert.Equal(ContentChannelItemStatus.PendingApproval, item.Status);
        Assert.False(item.IsPublishedAt(now));

        item.Moderate(ContentChannelItemStatus.Approved, Guid.CreateVersion7(), now);
        Assert.True(item.IsPublishedAt(now));
        Assert.False(item.IsPublishedAt(now.AddDays(6)));

        var childId = ContentChannelItemId.Create();
        Assert.True(item.AddAssociation(item.Id).IsFailure);
        Assert.True(item.AddAssociation(childId, order: 1).IsSuccess);
        _ = Assert.Single(item.ChildAssociations);

        var childChannel = ContentChannel.Create(type.Id, "Sub News", "sub-news").Value;
        channel.AddChildChannel(channel.Id);
        Assert.Empty(channel.ChildChannelIds);
        channel.AddChildChannel(childChannel.Id);
        channel.AddChildChannel(childChannel.Id);
        _ = Assert.Single(channel.ChildChannelIds);

        var retiredPerson = Guid.CreateVersion7();
        var survivingPerson = Guid.CreateVersion7();
        item.Moderate(ContentChannelItemStatus.Approved, retiredPerson, now);
        Assert.True(item.RepointPerson(retiredPerson, survivingPerson));
        Assert.Equal(survivingPerson, item.ApprovedByPersonId);
        Assert.False(item.RepointPerson(Guid.CreateVersion7(), survivingPerson));

        var rss = ContentChannelFeedGenerator.GenerateRss2(channel, [item], "https://ruth.local");
        var xmlDoc = XDocument.Parse(rss);
        Assert.Equal("rss", xmlDoc.Root?.Name.LocalName);
        Assert.Contains("Annual Summit", rss, StringComparison.Ordinal);

        var jsonFeed = ContentChannelFeedGenerator.GenerateJsonFeed11(
            channel,
            [item],
            "https://ruth.local"
        );
        using var jsonDoc = JsonDocument.Parse(jsonFeed);
        Assert.Equal(
            "https://jsonfeed.org/version/1.1",
            jsonDoc.RootElement.GetProperty("version").GetString()
        );
        Assert.Equal(1, jsonDoc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void BookmarksMediaAndPersistedDataset_EnforceOwnershipSyncAndStaleness()
    {
        Assert.True(
            PersonalLinkSection.Create("Personal", personId: null, isShared: false).IsFailure
        );

        var section = PersonalLinkSection.Create("Ops Shortcuts", isShared: true).Value;
        Assert.True(section.AddLink("Bad", "javascript:void(0)").IsFailure);
        Assert.True(section.AddLink("People", "/crm/people", "People").IsSuccess);
        _ = Assert.Single(section.Links);

        var personalOwner = Guid.CreateVersion7();
        var mergedOwner = Guid.CreateVersion7();
        var personalSection = PersonalLinkSection
            .Create("My Links", personId: personalOwner, isShared: false)
            .Value;
        Assert.True(personalSection.RepointPerson(personalOwner, mergedOwner));
        Assert.Equal(mergedOwner, personalSection.PersonId);
        Assert.False(personalSection.RepointPerson(Guid.CreateVersion7(), mergedOwner));

        var folder = MediaFolder
            .Create(MediaAccountId.Create(), "ext-1", "Keynotes", "Keynotes desc")
            .Value;
        folder.UpdateFromSync("Updated Keynotes", "New desc");
        Assert.Equal("Updated Keynotes", folder.Name);
        Assert.Equal("New desc", folder.Description);

        var now = DateTimeOffset.UtcNow;
        var ds = PersistedDataset
            .Create(
                "home-news",
                "Home News",
                PersistedDatasetSourceKind.ContentChannelFeed,
                refreshIntervalMinutes: 30
            )
            .Value;
        Assert.True(ds.IsStaleAt(now));
        ds.RecordRefresh("[]", 12, now);
        Assert.False(ds.IsStaleAt(now.AddMinutes(10)));
        Assert.True(ds.IsStaleAt(now.AddMinutes(31)));
    }
}
