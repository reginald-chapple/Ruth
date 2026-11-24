using System.Net;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier3;

[Collection(E2ETestCollection.Name)]
public sealed class PipelineAndOnboardingPairwiseTests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task OnboardingGate_Allows_StaticAssets_While_Unonboarded()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirectClient = host.CreateNoRedirectClient();

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            using var onboardingResponse = await noRedirectClient.GetAsync("/onboarding");
            Assert.Equal(HttpStatusCode.OK, onboardingResponse.StatusCode);

            var html = await onboardingResponse.Content.ReadAsStringAsync();
            var assetPaths = PairwiseTestSupport.ExtractStaticAssetPaths(html);
            Assert.NotEmpty(assetPaths);

            foreach (var assetPath in assetPaths)
            {
                using var assetResponse = await noRedirectClient.GetAsync(assetPath);
                Assert.Equal(HttpStatusCode.OK, assetResponse.StatusCode);
            }
        });
    }

    [Fact]
    public async Task OnboardingGate_Allows_HealthAndLiveness_While_Unonboarded()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirectClient = host.CreateNoRedirectClient();

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            using var liveResponse = await noRedirectClient.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            var liveBody = await liveResponse.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", liveBody, StringComparison.OrdinalIgnoreCase);

            using var healthResponse = await noRedirectClient.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        });
    }

    [Fact]
    public async Task OnboardingGate_And_OnboardingPost_Emit_OtelActivity()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            using var capture = RuthHostFactory.StartActivityCapture(
                "Ruth",
                "Microsoft.AspNetCore"
            );
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirectClient = host.CreateNoRedirectClient();

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");
            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(
                noRedirectClient
            );
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            using var homeResponse = await noRedirectClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

            var activities = capture.RecordedActivities;
            Assert.NotEmpty(activities);
            Assert.Contains(activities, a => a.TraceId != default && a.Duration >= TimeSpan.Zero);
        });
    }

    [Fact]
    public async Task InvalidOnboarding_Returns400ProblemDetails_And_KeepsGateUnlocked()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirectClient = host.CreateNoRedirectClient();

            using var invalidContent = TestOnboardingPayloads.ToJsonContent(
                TestOnboardingPayloads.CreateInvalidEmailPayload()
            );
            using var badResponse = await noRedirectClient.PostAsync(
                "/api/onboarding",
                invalidContent
            );
            var badBody = await badResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
            PairwiseTestSupport.AssertProblemDetailsResponse(badResponse, badBody);

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            Assert.Equal(0L, await probe.CountUserAccountsAsync());
            Assert.Equal(0L, await probe.CountOrganizationProfilesAsync());
        });
    }

    [Fact]
    public async Task RateLimiting_And_OutputCaching_PreserveHealthLive_AndTransitionGate()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirectClient = host.CreateNoRedirectClient();

            for (var i = 0; i < 12; i++)
            {
                using var liveResponse = await noRedirectClient.GetAsync("/health/live");
                Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            }

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(
                noRedirectClient
            );
            Assert.True(onboardDoc.RootElement.TryGetProperty("adminUserId", out _));

            using var postHome = await noRedirectClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, postHome.StatusCode);
            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/onboarding", "/");
        });
    }

    [Fact]
    public async Task AutoMigrateAndSeed_ZeroSeedStartup_LeavesTablesEmpty_AndEnforcesGate()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Bootstrap:AutoMigrateAndSeed"] = "true",
            };
            await using var host = RuthHostFactory.Create(
                _containers,
                databaseName,
                overrides: overrides
            );
            using var noRedirectClient = host.CreateNoRedirectClient();

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            Assert.True(await probe.SchemaExistsAsync("identity"));
            Assert.True(await probe.SchemaExistsAsync("scheduler"));
            Assert.Equal(0L, await probe.CountUserAccountsAsync());
            Assert.Equal(0L, await probe.CountOrganizationProfilesAsync());
            Assert.Equal(0L, await probe.CountIdentityOutboxMessagesAsync());
        });
    }
}
