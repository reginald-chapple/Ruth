using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Infrastructure;

[Collection(CmsFixtures.Name)]
public sealed class W6S1CmsEngineIntegrationTests(CmsContainersFixture containers)
{
    [Fact]
    public async Task W6_S1_DynamicCmsEngine_Postgres_EndToEnd_Works()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var sp = await CmsTestHostFactory.CreateAsync(dbConn);
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (siteId, blockId) = await ProvisionSitePageAndBlockAsync(commands);
        await VerifyHtmlApprovalAndRouteResolutionAsync(commands, queries, blockId);
        await VerifyShortLinkQrAndRequestFilterAsync(commands, queries, siteId);
    }

    private static async Task<(Guid SiteId, Guid BlockId)> ProvisionSitePageAndBlockAsync(
        ICommandDispatcher commands
    )
    {
        var theme = (
            await commands.SendAsync<CreateCmsThemeCommand, CmsThemeResponse>(
                new CreateCmsThemeCommand(
                    "Civic Light",
                    "civic-light",
                    CssCustomProperties: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["--cms-accent"] = "#0F6CBD",
                    }
                )
            )
        ).Value;

        var site = (
            await commands.SendAsync<CreateCmsSiteCommand, CmsSiteResponse>(
                new CreateCmsSiteCommand(
                    "Community Portal",
                    "community-portal",
                    ThemeId: theme.Id,
                    Domains: ["portal.civic.org"]
                )
            )
        ).Value;

        var layout = (
            await commands.SendAsync<CreateCmsLayoutCommand, CmsLayoutResponse>(
                new CreateCmsLayoutCommand(
                    site.Id,
                    "Main Layout",
                    "main-layout",
                    Zones: ["Hero", "Main"]
                )
            )
        ).Value;

        var page = (
            await commands.SendAsync<CreateCmsPageCommand, CmsPageResponse>(
                new CreateCmsPageCommand(
                    site.Id,
                    layout.Id,
                    "Member Detail",
                    "Member Spotlight",
                    Routes: ["members/{slug}"],
                    Contexts: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Member"] = "slug",
                    }
                )
            )
        ).Value;

        var block = (
            await commands.SendAsync<CreateCmsBlockCommand, CmsBlockResponse>(
                new CreateCmsBlockCommand(
                    "cms.html-content",
                    "Hero",
                    "Welcome Hero",
                    PageId: page.Id
                )
            )
        ).Value;

        return (site.Id, block.Id);
    }

    private static async Task VerifyHtmlApprovalAndRouteResolutionAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid blockId
    )
    {
        var v1 = (
            await commands.SendAsync<SaveHtmlContentVersionCommand, HtmlContentResponse>(
                new SaveHtmlContentVersionCommand(blockId, "<p>Draft v1</p>", AutoApprove: false)
            )
        ).Value;
        Assert.Equal(1, v1.Version);
        Assert.False(v1.IsApproved);

        var approved = (
            await commands.SendAsync<ApproveHtmlContentCommand, HtmlContentResponse>(
                new ApproveHtmlContentCommand(v1.Id, IsApproved: true, Guid.CreateVersion7())
            )
        ).Value;
        Assert.True(approved.IsApproved);

        var resolved = (
            await queries.QueryAsync<ResolveCmsPageQuery, ResolvedCmsPageResponse>(
                new ResolveCmsPageQuery(Host: "portal.civic.org", Path: "/members/avery-chen")
            )
        ).Value;
        Assert.Equal("Member Spotlight", resolved.Page.PageTitle);
        Assert.Equal("avery-chen", resolved.ResolvedContexts["Member"]);
        Assert.Equal("<p>Draft v1</p>", resolved.ActiveHtmlByBlockId[blockId].Content);
    }

    private static async Task VerifyShortLinkQrAndRequestFilterAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid siteId
    )
    {
        var sl = (
            await commands.SendAsync<CreatePageShortLinkCommand, PageShortLinkResponse>(
                new CreatePageShortLinkCommand(
                    siteId,
                    "welcome",
                    "/members/avery-chen",
                    UtmSource: "qr"
                )
            )
        ).Value;
        var clicked = (
            await commands.SendAsync<ResolvePageShortLinkCommand, PageShortLinkResponse>(
                new ResolvePageShortLinkCommand(sl.Token)
            )
        ).Value;
        Assert.Equal(1, clicked.ClickCount);
        Assert.Contains("utm_source=qr", clicked.TargetUrl, StringComparison.Ordinal);

        var qr = (
            await queries.QueryAsync<GetShortLinkQrQuery, ShortLinkQrResponse>(
                new GetShortLinkQrQuery(sl.Token, "https://portal.civic.org")
            )
        ).Value;
        Assert.StartsWith("<svg ", qr.QrSvgMarkup, StringComparison.Ordinal);

        var filter = (
            await commands.SendAsync<CreateRequestFilterCommand, RequestFilterResponse>(
                new CreateRequestFilterCommand(
                    "Mobile Campaign",
                    "mobile-campaign",
                    siteId,
                    RequestFilterMode.AllMatch,
                    [
                        new(
                            RequestFilterRuleKind.UserAgentDevice,
                            RequestFilterComparison.Equals,
                            "device",
                            "Mobile"
                        ),
                        new(
                            RequestFilterRuleKind.QueryString,
                            RequestFilterComparison.Equals,
                            "utm",
                            "spring"
                        ),
                    ]
                )
            )
        ).Value;

        var evalRes = (
            await queries.QueryAsync<EvaluateRequestFilterQuery, RequestFilterEvaluationResponse>(
                new EvaluateRequestFilterQuery(
                    filter.Key,
                    new RequestEvaluationContext(
                        UserAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0) Mobile",
                        QueryParameters: new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["utm"] = "spring",
                        }
                    )
                )
            )
        ).Value;
        Assert.True(evalRes.IsMatch);
    }
}
