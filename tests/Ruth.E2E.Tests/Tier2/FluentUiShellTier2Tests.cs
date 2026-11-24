using System.Net;
using System.Text.RegularExpressions;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed partial class FluentUiShellTier2Tests(E2EContainersFixture containers)
{
    private static readonly string[] _legacyFluentV4Markers =
    [
        "FluentDesignTheme",
        "FluentNavMenu",
        "fluent-design-theme",
        "fluent-nav-menu",
    ];

    private static readonly string[] _fluentProviderMarkers =
    [
        "fluent-providers",
        "fluent-dialog-provider",
        "fluent-toast-provider",
        "fluent-tooltip-provider",
    ];

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Shell_OnboardedRootHtml_ContainsNoLegacyFluentUiV4Markers()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();

            foreach (var marker in _legacyFluentV4Markers)
            {
                Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var providerMarker in _fluentProviderMarkers)
            {
                Assert.Contains(providerMarker, html, StringComparison.OrdinalIgnoreCase);
            }

            await AssertAllRootStylesheetsReturn200Async(client, html);
        });
    }

    [Fact]
    public async Task Shell_OnboardingHtml_ContainsNoLegacyFluentUiV4Markers()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var response = await client.GetAsync(new Uri("/onboarding", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            foreach (var marker in _legacyFluentV4Markers)
            {
                Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
            }
        });
    }

    [Fact]
    public async Task Shell_MissingStaticAssetPostOnboarding_Returns404WithoutRedirectLoop()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var cssResp = await client.GetAsync(
                new Uri("/css/nonexistent-boundary-style.css", UriKind.Relative)
            );
            using var jsResp = await client.GetAsync(
                new Uri("/_content/MissingLib/nonexistent.js", UriKind.Relative)
            );

            Assert.Equal(HttpStatusCode.NotFound, cssResp.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, jsResp.StatusCode);
        });
    }

    [Fact]
    public async Task Shell_UnknownAppRoutePostOnboarding_HandlesCleanlyWithoutServerError()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(
                new Uri("/unknown-shell-route-boundary-check", UriKind.Relative)
            );
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("Page not found", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(
                "The requested page could not be found.",
                body,
                StringComparison.OrdinalIgnoreCase
            );
            Tier2TestHelpers.AssertNoStackTraceLeak(body);
        });
    }

    [Fact]
    public async Task Shell_OnboardingStaticCssAndBlazorScripts_Load200WithoutCircularRedirect()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();

            using var pageResp = await client.GetAsync(new Uri("/onboarding", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, pageResp.StatusCode);
            var html = await pageResp.Content.ReadAsStringAsync();

            var cssHrefs = CssHrefRegex
                .Matches(html)
                .Select(m => m.Groups["href"].Value)
                .Where(h => h.StartsWith('/'))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.NotEmpty(cssHrefs);
            foreach (var href in cssHrefs)
            {
                using var assetResp = await client.GetAsync(new Uri(href, UriKind.Relative));
                Assert.Equal(HttpStatusCode.OK, assetResp.StatusCode);
            }

            var blazorJsSrcs = BlazorScriptSrcRegex
                .Matches(html)
                .Select(m => m.Groups["src"].Value)
                .Select(s => s.StartsWith('/') ? s : $"/{s}")
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.NotEmpty(blazorJsSrcs);
            foreach (var src in blazorJsSrcs)
            {
                using var blazorJs = await client.GetAsync(new Uri(src, UriKind.Relative));
                Assert.Equal(HttpStatusCode.OK, blazorJs.StatusCode);
            }
        });
    }

    [Fact]
    public async Task Shell_OnboardingPostWithoutAntiforgeryToken_DoesNotBypassProtection()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();

            using var form = new FormUrlEncodedContent(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["OrganizationName"] = "Unverified Form Post",
                }
            );
            using var resp = await client.PostAsync(new Uri("/onboarding", UriKind.Relative), form);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);
        });
    }

    private static async Task AssertAllRootStylesheetsReturn200Async(HttpClient client, string html)
    {
        var cssHrefs = CssHrefRegex
            .Matches(html)
            .Select(m => m.Groups["href"].Value)
            .Select(h => h.StartsWith('/') ? h : $"/{h}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Contains(cssHrefs, h =>
            h.Contains("default-fuib", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(cssHrefs, h =>
            h.Contains("Ruth.Host", StringComparison.OrdinalIgnoreCase)
            && h.Contains("styles.css", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(cssHrefs, h =>
            h.Contains("app.", StringComparison.OrdinalIgnoreCase)
            && h.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
        );

        foreach (var href in cssHrefs)
        {
            using var cssResp = await client.GetAsync(new Uri(href, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, cssResp.StatusCode);
        }
    }

    [GeneratedRegex(
        """<link[^>]+href=["'](?<href>[^"']+\.css[^"']*)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex CssHrefRegex { get; }

    [GeneratedRegex(
        """<script[^>]+src=["'](?<src>[^"']*blazor\.web[^"']*\.js)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex BlazorScriptSrcRegex { get; }
}
