using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Tests.Fixtures;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Infrastructure;

[Collection(CmsFixtures.Name)]
public sealed class W6S2CmsContentAndMediaIntegrationTests(CmsContainersFixture containers)
{
    [Fact]
    public async Task W6_S2_ContentChannels_MediaSync_Bookmarks_AndDatasets_Work()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var sp = await CmsTestHostFactory.CreateAsync(dbConn);
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await VerifyChannelsModerationAndFeedsAsync(commands, queries);
        await VerifyMediaSyncBookmarksAndDatasetsAsync(commands, queries, sp);
    }

    private static async Task VerifyChannelsModerationAndFeedsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var chType = (
            await commands.SendAsync<CreateContentChannelTypeCommand, ContentChannelTypeResponse>(
                new CreateContentChannelTypeCommand("Articles", "articles")
            )
        ).Value;

        var channel = (
            await commands.SendAsync<CreateContentChannelCommand, ContentChannelResponse>(
                new CreateContentChannelCommand(
                    chType.Id,
                    "Community News",
                    "community-news",
                    RequiresApproval: true
                )
            )
        ).Value;

        var topic = (
            await commands.SendAsync<CreateContentTopicCommand, ContentTopicResponse>(
                new CreateContentTopicCommand("Programs", "programs")
            )
        ).Value;

        var item1 = (
            await commands.SendAsync<CreateContentChannelItemCommand, ContentChannelItemResponse>(
                new CreateContentChannelItemCommand(
                    channel.Id,
                    "Spring Grant Open",
                    "spring-grant-open",
                    "<p>Applications open now.</p>",
                    Summary: "Grants open",
                    PrimaryTopicId: topic.Id
                )
            )
        ).Value;
        Assert.Equal(ContentChannelItemStatus.PendingApproval, item1.Status);

        var approvedItem = (
            await commands.SendAsync<ModerateContentChannelItemCommand, ContentChannelItemResponse>(
                new ModerateContentChannelItemCommand(item1.Id, ContentChannelItemStatus.Approved)
            )
        ).Value;
        Assert.Equal(ContentChannelItemStatus.Approved, approvedItem.Status);

        var rss = (
            await queries.QueryAsync<GetContentChannelFeedQuery, ContentChannelFeedResponse>(
                new GetContentChannelFeedQuery("community-news", "rss")
            )
        ).Value;
        Assert.Equal(1, rss.ItemCount);
        Assert.Contains("Spring Grant Open", rss.Payload, StringComparison.Ordinal);

        var jsonFeed = (
            await queries.QueryAsync<GetContentChannelFeedQuery, ContentChannelFeedResponse>(
                new GetContentChannelFeedQuery("community-news", "json")
            )
        ).Value;
        Assert.Contains(
            "https://jsonfeed.org/version/1.1",
            jsonFeed.Payload,
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyMediaSyncBookmarksAndDatasetsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ServiceProvider sp
    )
    {
        var mediaAcc = (
            await commands.SendAsync<CreateMediaAccountCommand, MediaAccountResponse>(
                new CreateMediaAccountCommand("Org Media", "org-media", "in-memory", "org-channel")
            )
        ).Value;
        var synced = (
            await commands.SendAsync<SyncMediaAccountCommand, MediaAccountResponse>(
                new SyncMediaAccountCommand(mediaAcc.Id)
            )
        ).Value;
        Assert.True(synced.FolderCount >= 1);
        Assert.True(synced.ElementCount >= 2);

        var elements = (
            await queries.QueryAsync<GetMediaElementsQuery, IReadOnlyList<MediaElementResponse>>(
                new GetMediaElementsQuery(MediaAccountId: mediaAcc.Id)
            )
        ).Value;
        Assert.True(elements.Count >= 2);

        var bookmarkSec = (
            await commands.SendAsync<CreatePersonalLinkSectionCommand, PersonalLinkSectionResponse>(
                new CreatePersonalLinkSectionCommand("Shared Ops", IsShared: true)
            )
        ).Value;
        var withLink = (
            await commands.SendAsync<AddPersonalLinkCommand, PersonalLinkSectionResponse>(
                new AddPersonalLinkCommand(bookmarkSec.Id, "CMS Sites", "/cms/sites")
            )
        ).Value;
        _ = Assert.Single(withLink.Links);

        var dataset = (
            await commands.SendAsync<CreatePersistedDatasetCommand, PersistedDatasetResponse>(
                new CreatePersistedDatasetCommand(
                    "news-cache",
                    "Cached News",
                    PersistedDatasetSourceKind.ContentChannelFeed,
                    SourceConfiguration: new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["ChannelSlug"] = "community-news",
                    }
                )
            )
        ).Value;
        Assert.Contains("Spring Grant Open", dataset.ResultDataJson, StringComparison.Ordinal);

        var job = sp.GetServices<IScheduledJob>()
            .Single(static j =>
                string.Equals(j.JobKey, "cms.persisted-datasets.refresh", StringComparison.Ordinal)
            );
        var jobResult = await job.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobResult.Status);
    }
}
