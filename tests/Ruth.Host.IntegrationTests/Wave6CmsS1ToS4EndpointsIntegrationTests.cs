using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave6CmsS1ToS4EndpointsIntegrationTests(HostContainersFixture containers)
{
    private static readonly string[] _portalDomains = ["host-portal.ruth.local"];
    private static readonly string[] _layoutZones = ["Hero", "Main"];
    private static readonly string[] _rootRoutes = [""];
    private static readonly string[] _signageFeedPaths =
    [
        "/api/v1/signage/host-lobby-signage/feed",
        "/api/v1/signage/host-lobby-signage/apple-tv",
        "/api/v1/signage/host-lobby-signage/roku",
    ];
    private static readonly string[] _blazorRoutes =
    [
        "/cms/sites",
        "/cms/content",
        "/cms/personalization",
        "/cms/apps",
        "/cms/preview/host-portal/",
    ];

    [Fact]
    public async Task Cms_Engine_Channels_Personalization_MobileSignage_OpenApi_AndBlazorPages_WorkEndToEnd()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bootstrap:AutoMigrateAndSeed"] = "true",
                ["OpenApi:Enabled"] = "true",
                ["Cms:InteractionFlushWorkerEnabled"] = "false",
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await Wave4CommerceHttpSeedHelper.OnboardAndLoginAsync(client);
        var siteId = await VerifyW6S1HttpAsync(client, token);
        var (channelId, folderId) = await VerifyW6S2HttpAsync(client, token);
        await VerifyW6S3HttpAsync(client, token);
        await VerifyW6S4HttpAsync(client, token, siteId, channelId, folderId);
        await VerifyOpenApiAndBlazorRoutesAsync(client);
    }

    private static async Task<Guid> VerifyW6S1HttpAsync(HttpClient client, string token)
    {
        using var themeResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/themes",
            new
            {
                name = "Host Portal Theme",
                slug = "host-portal-theme",
                cssCustomProperties = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["--cms-accent"] = "#0F6CBD",
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, themeResp.StatusCode);
        using var themeDoc = JsonDocument.Parse(await themeResp.Content.ReadAsStringAsync());
        var themeId = themeDoc.RootElement.GetProperty("id").GetGuid();

        using var cssResp = await client.GetAsync(
            new Uri("/api/cms/themes/host-portal-theme.css", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, cssResp.StatusCode);
        Assert.Contains(
            "--cms-accent: #0F6CBD;",
            await cssResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var siteResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/sites",
            new
            {
                name = "Host Portal",
                slug = "host-portal",
                themeId,
                domains = _portalDomains,
            }
        );
        Assert.Equal(HttpStatusCode.Created, siteResp.StatusCode);
        using var siteDoc = JsonDocument.Parse(await siteResp.Content.ReadAsStringAsync());
        var siteId = siteDoc.RootElement.GetProperty("id").GetGuid();

        using var layoutResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/layouts",
            new
            {
                siteId,
                name = "Full Width",
                slug = "full-width",
                zones = _layoutZones,
            }
        );
        Assert.Equal(HttpStatusCode.Created, layoutResp.StatusCode);
        using var layoutDoc = JsonDocument.Parse(await layoutResp.Content.ReadAsStringAsync());
        var layoutId = layoutDoc.RootElement.GetProperty("id").GetGuid();

        using var pageResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/pages",
            new
            {
                siteId,
                layoutId,
                internalName = "Home",
                pageTitle = "Host Community Home",
                setAsSiteDefault = true,
                routes = _rootRoutes,
            }
        );
        Assert.Equal(HttpStatusCode.Created, pageResp.StatusCode);
        using var pageDoc = JsonDocument.Parse(await pageResp.Content.ReadAsStringAsync());
        var pageId = pageDoc.RootElement.GetProperty("id").GetGuid();

        using var blockResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/blocks",
            new
            {
                blockTypeKey = "cms.html-content",
                zone = "Hero",
                name = "Hero Block",
                pageId,
            }
        );
        Assert.Equal(HttpStatusCode.Created, blockResp.StatusCode);
        using var blockDoc = JsonDocument.Parse(await blockResp.Content.ReadAsStringAsync());
        var blockId = blockDoc.RootElement.GetProperty("id").GetGuid();

        using var htmlResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/cms/blocks/{blockId:D}/html",
            new
            {
                blockId,
                content = "<h2>Hello Host CMS</h2>",
                autoApprove = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, htmlResp.StatusCode);

        using var resolveResp = await client.GetAsync(
            new Uri("/api/cms/public/resolve?siteSlug=host-portal", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        Assert.Contains(
            "Hello Host CMS",
            await resolveResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var slResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/shortlinks",
            new
            {
                siteId,
                token = "welcome-host",
                targetUrl = "/cms/preview/host-portal/",
            }
        );
        Assert.Equal(HttpStatusCode.Created, slResp.StatusCode);

        using var qrResp = await client.GetAsync(
            new Uri("/api/cms/public/shortlinks/welcome-host/qr.svg", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, qrResp.StatusCode);
        Assert.Contains("<svg", await qrResp.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var goResp = await client.GetAsync(new Uri("/go/welcome-host", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Found, goResp.StatusCode);
        Assert.Equal("/cms/preview/host-portal/", goResp.Headers.Location?.ToString());

        return siteId;
    }

    private static async Task<(Guid ChannelId, Guid FolderId)> VerifyW6S2HttpAsync(
        HttpClient client,
        string token
    )
    {
        using var typeResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/channel-types",
            new { name = "Updates", key = "updates" }
        );
        Assert.Equal(HttpStatusCode.Created, typeResp.StatusCode);
        using var typeDoc = JsonDocument.Parse(await typeResp.Content.ReadAsStringAsync());
        var typeId = typeDoc.RootElement.GetProperty("id").GetGuid();

        using var chResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/channels",
            new
            {
                contentChannelTypeId = typeId,
                name = "Host News",
                slug = "host-news",
            }
        );
        Assert.Equal(HttpStatusCode.Created, chResp.StatusCode);
        using var chDoc = JsonDocument.Parse(await chResp.Content.ReadAsStringAsync());
        var channelId = chDoc.RootElement.GetProperty("id").GetGuid();

        using var itemResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/channels/items",
            new
            {
                contentChannelId = channelId,
                title = "Welcome Release",
                slug = "welcome-release",
                content = "<p>Live news item.</p>",
            }
        );
        Assert.Equal(HttpStatusCode.Created, itemResp.StatusCode);

        using var rssResp = await client.GetAsync(
            new Uri("/api/cms/channels/host-news/feed.rss", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, rssResp.StatusCode);
        Assert.Contains(
            "Welcome Release",
            await rssResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var jsonResp = await client.GetAsync(
            new Uri("/api/cms/channels/host-news/feed.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, jsonResp.StatusCode);
        Assert.Contains(
            "jsonfeed.org/version/1.1",
            await jsonResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var mediaAccResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/media/accounts",
            new
            {
                name = "Host Media",
                slug = "host-media",
                providerKey = "in-memory",
                accountIdentifier = "host-org",
            }
        );
        Assert.Equal(HttpStatusCode.Created, mediaAccResp.StatusCode);
        using var mediaDoc = JsonDocument.Parse(await mediaAccResp.Content.ReadAsStringAsync());
        var mediaAccId = mediaDoc.RootElement.GetProperty("id").GetGuid();

        using var syncResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/cms/media/accounts/{mediaAccId:D}/sync",
            new { }
        );
        Assert.Equal(HttpStatusCode.OK, syncResp.StatusCode);

        using var foldersResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Get,
            $"/api/cms/media/folders?mediaAccountId={mediaAccId:D}",
            new { }
        );
        Assert.Equal(HttpStatusCode.OK, foldersResp.StatusCode);
        using var foldersDoc = JsonDocument.Parse(await foldersResp.Content.ReadAsStringAsync());
        var folderId = foldersDoc.RootElement[0].GetProperty("id").GetGuid();

        using var dsResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/datasets",
            new
            {
                accessKey = "host-news-cache",
                name = "Host News Cache",
                dataSourceKind = "ContentChannelFeed",
                sourceConfiguration = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["ChannelSlug"] = "host-news",
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, dsResp.StatusCode);

        using var dsPublic = await client.GetAsync(
            new Uri("/api/cms/datasets/host-news-cache", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, dsPublic.StatusCode);
        Assert.Contains(
            "Welcome Release",
            await dsPublic.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        return (channelId, folderId);
    }

    private static async Task VerifyW6S3HttpAsync(HttpClient client, string token)
    {
        using var ingestResp = await client.PostAsJsonAsync(
            new Uri("/api/cms/public/interactions/ingest", UriKind.Relative),
            new
            {
                flushImmediately = true,
                items = new object[]
                {
                    new
                    {
                        channelKey = "host-web",
                        componentKey = "home",
                        componentName = "Home",
                        operation = "View",
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Accepted, ingestResp.StatusCode);

        using var msgResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/personalization/adaptive-messages",
            new { name = "Host Hero Banner", key = "host-hero-banner" }
        );
        Assert.Equal(HttpStatusCode.Created, msgResp.StatusCode);
        using var msgDoc = JsonDocument.Parse(await msgResp.Content.ReadAsStringAsync());
        var msgId = msgDoc.RootElement.GetProperty("id").GetGuid();

        using var adaptResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/cms/personalization/adaptive-messages/{msgId:D}/adaptations",
            new
            {
                adaptiveMessageId = msgId,
                name = "Default Hero",
                headline = "Welcome Community",
                bodyMarkup = "Explore member tools",
                order = 0,
            }
        );
        Assert.Equal(HttpStatusCode.OK, adaptResp.StatusCode);

        using var resolveResp = await client.PostAsJsonAsync(
            new Uri("/api/cms/public/adaptive-messages/host-hero-banner/resolve", UriKind.Relative),
            new { host = "host-portal.ruth.local" }
        );
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        Assert.Contains(
            "Welcome Community",
            await resolveResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyW6S4HttpAsync(
        HttpClient client,
        string token,
        Guid siteId,
        Guid channelId,
        Guid folderId
    )
    {
        using var mobResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/mobile-apps",
            new
            {
                name = "Host Mobile",
                slug = "host-mobile",
                bundleIdentifier = "org.ruth.hostmobile",
                siteId,
            }
        );
        Assert.Equal(HttpStatusCode.Created, mobResp.StatusCode);
        using var mobDoc = JsonDocument.Parse(await mobResp.Content.ReadAsStringAsync());
        var mobId = mobDoc.RootElement.GetProperty("id").GetGuid();

        using var pageResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/cms/mobile-apps/{mobId:D}/pages",
            new
            {
                mobileApplicationId = mobId,
                title = "Feed",
                pageKey = "feed",
                route = "/feed",
                contentChannelId = channelId,
                adaptiveMessageKey = "host-hero-banner",
            }
        );
        Assert.Equal(HttpStatusCode.OK, pageResp.StatusCode);

        using var bootResp = await client.GetAsync(
            new Uri("/api/v1/mobile/apps/host-mobile/bootstrap", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, bootResp.StatusCode);

        using var hydResp = await client.GetAsync(
            new Uri("/api/v1/mobile/apps/host-mobile/pages/feed", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, hydResp.StatusCode);
        Assert.Contains(
            "Welcome Release",
            await hydResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var sigResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/cms/signage-apps",
            new
            {
                name = "Host Lobby Signage",
                slug = "host-lobby-signage",
                platform = "AppleTv",
                siteId,
                defaultContentChannelId = channelId,
                mediaFolderId = folderId,
                slides = new object[]
                {
                    new
                    {
                        title = "Lobby Welcome",
                        durationSeconds = 15,
                        order = 0,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, sigResp.StatusCode);

        foreach (var path in _signageFeedPaths)
        {
            using var feedResp = await client.GetAsync(new Uri(path, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, feedResp.StatusCode);
        }
    }

    private static async Task VerifyOpenApiAndBlazorRoutesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/cms/sites", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/cms/channels/{channelSlug}/feed.rss",
            openApiText,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "/api/cms/public/interactions/ingest",
            openApiText,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "/api/v1/mobile/apps/{appSlug}/bootstrap",
            openApiText,
            StringComparison.Ordinal
        );
        Assert.Contains("/api/v1/signage/{appSlug}/roku", openApiText, StringComparison.Ordinal);

        foreach (var route in _blazorRoutes)
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
