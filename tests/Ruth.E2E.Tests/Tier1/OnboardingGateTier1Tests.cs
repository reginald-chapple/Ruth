using System.Globalization;
using System.Net;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class OnboardingGateTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _lockColumns =
    [
        "IsSelfRegistrationLocked",
        "is_self_registration_locked",
        "SelfRegistrationLocked",
        "self_registration_locked",
    ];

    [Fact]
    public async Task Feature15_ZeroSeedHome_Redirects302ToOnboarding()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();

        using var response = await noRedirectClient.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.EndsWith("/onboarding", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature15_ZeroSeedOnboardingPage_Returns200WithSetupForm()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();

        using var response = await noRedirectClient.GetAsync(
            new Uri("/onboarding", UriKind.Relative)
        );
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("onboarding", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-text-input", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-dropdown", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-option", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-button", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fluent-text-field", html, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            html.Contains("<form", StringComparison.OrdinalIgnoreCase)
                || html.Contains("organization", StringComparison.OrdinalIgnoreCase),
            "Expected onboarding setup form markup on /onboarding."
        );
    }

    [Fact]
    public async Task Feature15_GetApiOnboardingStatus_TransitionsPreAndPostOnboarding()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();

        using var beforeResponse = await noRedirectClient.GetAsync(
            new Uri("/api/onboarding/status", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
        using (var beforeDoc = JsonDocument.Parse(await beforeResponse.Content.ReadAsStringAsync()))
        {
            Assert.True(beforeDoc.RootElement.GetProperty("isOnboardingRequired").GetBoolean());
            Assert.False(
                beforeDoc.RootElement.GetProperty("isSelfRegistrationLocked").GetBoolean()
            );
        }

        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(
            noRedirectClient,
            TestOnboardingPayloads.CreateValidPayload(organizationName: "Status Check Org")
        );
        Assert.NotNull(onboardingDoc);

        using var afterResponse = await noRedirectClient.GetAsync(
            new Uri("/api/onboarding/status", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);
        using var afterDoc = JsonDocument.Parse(await afterResponse.Content.ReadAsStringAsync());
        Assert.False(afterDoc.RootElement.GetProperty("isOnboardingRequired").GetBoolean());
        Assert.True(afterDoc.RootElement.GetProperty("isSelfRegistrationLocked").GetBoolean());
        Assert.Equal(
            "Status Check Org",
            afterDoc.RootElement.GetProperty("organizationName").GetString()
        );
    }

    [Fact]
    public async Task Feature15_ZeroSeedExemptRoutes_DoNotRedirect()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();

        using var liveResponse = await noRedirectClient.GetAsync(
            new Uri("/health/live", UriKind.Relative)
        );
        using var readyResponse = await noRedirectClient.GetAsync(
            new Uri("/health", UriKind.Relative)
        );

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var onboardingHtml = await noRedirectClient.GetStringAsync(
            new Uri("/onboarding", UriKind.Relative)
        );
        var frameworkAssetPath = Tier1TestSupport.ExtractFirstAssetPath(
            onboardingHtml,
            "_framework/"
        );
        var contentAssetPath = Tier1TestSupport.ExtractFirstAssetPath(onboardingHtml, "_content/");
        Assert.NotNull(frameworkAssetPath);
        Assert.NotNull(contentAssetPath);

        using var frameworkResponse = await noRedirectClient.GetAsync(
            new Uri(frameworkAssetPath, UriKind.Relative)
        );
        using var contentResponse = await noRedirectClient.GetAsync(
            new Uri(contentAssetPath, UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, frameworkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, contentResponse.StatusCode);
    }

    [Fact]
    public async Task Feature15_PostApiOnboarding_CompletesWith200Or201AndIds()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var doc = await Tier1TestSupport.CompleteOnboardingAsync(client);

        var orgId = doc.RootElement.GetProperty("organizationId").GetGuid();
        var adminUserId = doc.RootElement.GetProperty("adminUserId").GetGuid();
        var completedOnUtc = DateTimeOffset.Parse(
            doc.RootElement.GetProperty("completedOnUtc").GetString() ?? string.Empty,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind
        );

        TestOnboardingPayloads.AssertIsUuidV7(orgId);
        TestOnboardingPayloads.AssertIsUuidV7(adminUserId);
        TestOnboardingPayloads.AssertUtcTimestamp(completedOnUtc);
    }

    [Fact]
    public async Task Feature15_PostOnboarding_LocksSelfRegistrationAndServesHome()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(noRedirectClient);
        Assert.NotNull(onboardingDoc);

        using var homeResponse = await noRedirectClient.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

        var orgRow = Assert.Single(await scope.Probe.ReadOrganizationProfilesAsync());
        Assert.True(Tier1TestSupport.TryGetAnyColumn(orgRow, _lockColumns, out var lockedRaw));
        Assert.True(Convert.ToBoolean(lockedRaw, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Feature15_PostOnboarding_RedirectsOnboardingPageToHome()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var noRedirectClient = scope.Factory.CreateNoRedirectClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(noRedirectClient);
        Assert.NotNull(onboardingDoc);

        using var onboardingResponse = await noRedirectClient.GetAsync(
            new Uri("/onboarding", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.Found, onboardingResponse.StatusCode);
        var location = onboardingResponse.Headers.Location?.ToString() ?? string.Empty;
        Assert.True(
            string.Equals(location, "/", StringComparison.Ordinal) || location.EndsWith('/'),
            $"Expected /onboarding to redirect to '/', got '{location}'."
        );
    }
}
