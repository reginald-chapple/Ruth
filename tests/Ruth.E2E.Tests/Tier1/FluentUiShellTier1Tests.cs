using System.Net;
using System.Text.RegularExpressions;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed partial class FluentUiShellTier1Tests(E2EContainersFixture containers)
{
    [Fact]
    public async Task Feature12_HomePage_AfterOnboarding_Returns200Html()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateNoRedirectClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Feature12_AppShell_IncludesFluentUiV5CssAndBlazorWebJs()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("default-fuib", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("blazor.web", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature12_AppShell_RendersFluentLayoutAndNavigationMarkup()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("fluent-layout", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-providers", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-dialog-provider", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-toast-provider", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-tooltip-provider", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature12_AppShell_IncludesThemeToggleWithBlazorThemeSwitch()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("Blazor.theme.switchTheme", html, StringComparison.Ordinal);
        Assert.True(
            html.Contains("hidden-if-light", StringComparison.OrdinalIgnoreCase)
                || html.Contains("hidden-if-dark", StringComparison.OrdinalIgnoreCase),
            "Expected Fluent UI v5 theme utility classes (hidden-if-light/hidden-if-dark)."
        );
    }

    [Fact]
    public async Task Feature12_AppShell_RendersWithoutV4FluentDesignThemeOrErrorFallback()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.DoesNotContain("fluent-design-theme", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentDesignTheme", html, StringComparison.Ordinal);
        Assert.DoesNotContain("An unhandled error has occurred", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Feature12_StaticShellAssets_AreFetchableWith200Ok()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
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

        foreach (var cssPath in cssHrefs)
        {
            using var cssResponse = await client.GetAsync(new Uri(cssPath, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, cssResponse.StatusCode);
        }

        var jsPath = Tier1TestSupport.ExtractFirstAssetPath(html, "blazor.web");
        Assert.NotNull(jsPath);

        using var jsResponse = await client.GetAsync(new Uri(jsPath, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, jsResponse.StatusCode);
    }

    [GeneratedRegex(
        """<link[^>]+href=["'](?<href>[^"']+\.css[^"']*)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex CssHrefRegex { get; }
}
