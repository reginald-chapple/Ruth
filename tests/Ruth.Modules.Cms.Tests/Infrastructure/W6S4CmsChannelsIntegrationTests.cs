using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Infrastructure;

[Collection(CmsFixtures.Name)]
public sealed class W6S4CmsChannelsIntegrationTests(CmsContainersFixture containers)
{
    [Fact]
    public async Task W6_S4_MobileShellAndSignageTvFeeds_Postgres_Work()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var sp = await CmsTestHostFactory.CreateAsync(dbConn);
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await VerifyMobileAppContractAsync(commands, queries);
        await VerifySignageAndTvFeedsAsync(commands, queries);
    }

    private static async Task VerifyMobileAppContractAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var mobileApp = (
            await commands.SendAsync<CreateMobileApplicationCommand, MobileApplicationResponse>(
                new CreateMobileApplicationCommand(
                    "Member App",
                    "member-app",
                    "org.ruth.app",
                    MinimumClientVersion: "1.5.0",
                    LatestClientVersion: "1.8.0"
                )
            )
        ).Value;

        var withLayout = (
            await commands.SendAsync<AddMobileLayoutCommand, MobileApplicationResponse>(
                new AddMobileLayoutCommand(
                    mobileApp.Id,
                    "Main Shell",
                    "main-shell",
                    "<StackLayout />"
                )
            )
        ).Value;

        var withPage = (
            await commands.SendAsync<AddMobilePageCommand, MobileApplicationResponse>(
                new AddMobilePageCommand(
                    mobileApp.Id,
                    "News",
                    "news",
                    "/news",
                    withLayout.Layouts[0].Id
                )
            )
        ).Value;

        _ = await commands.SendAsync<AddMobileDeepLinkCommand, MobileApplicationResponse>(
            new AddMobileDeepLinkCommand(
                mobileApp.Id,
                "/app/news",
                "https://portal.civic.org/news",
                withPage.Pages[0].Id
            )
        );

        var bootstrap = (
            await queries.QueryAsync<GetMobileAppBootstrapQuery, MobileAppBootstrapResponse>(
                new GetMobileAppBootstrapQuery("member-app", ClientVersion: "1.2.0")
            )
        ).Value;
        Assert.True(bootstrap.ClientUpgradeRequired);
        _ = Assert.Single(bootstrap.NavigationPages);

        var deep = (
            await queries.QueryAsync<ResolveMobileDeepLinkQuery, ResolvedMobileDeepLinkResponse>(
                new ResolveMobileDeepLinkQuery("member-app", "/app/news/spring-grant-open")
            )
        ).Value;
        Assert.True(deep.Matched);
        Assert.Equal("news", deep.TargetPageKey);
    }

    private static async Task VerifySignageAndTvFeedsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var mediaAcc = (
            await commands.SendAsync<CreateMediaAccountCommand, MediaAccountResponse>(
                new CreateMediaAccountCommand("Lobby Media", "lobby-media", "in-memory", "lobby")
            )
        ).Value;
        _ = await commands.SendAsync<SyncMediaAccountCommand, MediaAccountResponse>(
            new SyncMediaAccountCommand(mediaAcc.Id)
        );
        var folders = (
            await queries.QueryAsync<GetMediaFoldersQuery, IReadOnlyList<MediaFolderResponse>>(
                new GetMediaFoldersQuery(mediaAcc.Id)
            )
        ).Value;

        var signage = (
            await commands.SendAsync<CreateSignageApplicationCommand, SignageApplicationResponse>(
                new CreateSignageApplicationCommand(
                    "Main Lobby TV",
                    "main-lobby-tv",
                    SignagePlatformType.Roku,
                    MediaFolderId: folders[0].Id,
                    Slides: [new SignageSlideSpec("Welcome", "Main Lobby")]
                )
            )
        ).Value;

        var roku = (
            await queries.QueryAsync<GetSignageFeedQuery, SignageFeedResponse>(
                new GetSignageFeedQuery(signage.Slug, "roku")
            )
        ).Value;
        using var rokuDoc = JsonDocument.Parse(roku.PayloadJson);
        Assert.True(rokuDoc.RootElement.GetProperty("shortFormVideos").GetArrayLength() >= 2);
    }
}
