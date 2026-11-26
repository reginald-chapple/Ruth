using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier4;

[Collection(E2ETestCollection.Name)]
public sealed class Wave0BootstrapAndShellWorkflowTests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Scenario1_ZeroSeedColdBoot_OnboardingWizard_PermanentLock_AndFluentUiShell()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var noRedirect = host.CreateNoRedirectClient();

            await WorkflowTestSupport.AssertRedirectAsync(noRedirect, "/", "/onboarding");

            using var wizardResp = await noRedirect.GetAsync("/onboarding");
            Assert.Equal(HttpStatusCode.OK, wizardResp.StatusCode);
            var wizardHtml = await wizardResp.Content.ReadAsStringAsync();
            Assert.Contains("default-fuib", wizardHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("blazor.web", wizardHtml, StringComparison.OrdinalIgnoreCase);

            using var statusBefore = await noRedirect.GetAsync("/api/onboarding/status");
            Assert.Equal(HttpStatusCode.OK, statusBefore.StatusCode);
            Assert.Contains(
                "true",
                await statusBefore.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase
            );

            using var onboardDoc = await WorkflowTestSupport.CompleteOnboardingAsync(noRedirect);
            var orgId = onboardDoc.RootElement.GetProperty("organizationId").GetGuid();
            var adminId = onboardDoc.RootElement.GetProperty("adminUserId").GetGuid();
            TestOnboardingPayloads.AssertIsUuidV7(orgId);
            TestOnboardingPayloads.AssertIsUuidV7(adminId);

            await AssertPostOnboardingShellAndLockAsync(noRedirect);
        });
    }

    [Fact]
    public async Task Scenario2_FullStackReadiness_OpenApiMetadata_AndOtelTraceCapture()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            using var capture = RuthHostFactory.StartActivityCapture(
                "Ruth",
                "Microsoft.AspNetCore"
            );
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var extensions = await probe.GetInstalledExtensionsAsync();
            Assert.Contains("postgis", extensions, StringComparer.Ordinal);
            Assert.Contains("vector", extensions, StringComparer.Ordinal);
            Assert.DoesNotContain("age", extensions, StringComparer.Ordinal);

            using var liveResp = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveResp.StatusCode);

            using var healthResp = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthResp.StatusCode);

            using var openApiResp = await client.GetAsync("/openapi/v1.json");
            Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
            var openApiJson = await openApiResp.Content.ReadAsStringAsync();
            using var openApiDoc = JsonDocument.Parse(openApiJson);
            Assert.True(openApiDoc.RootElement.TryGetProperty("paths", out _));
            Assert.Contains("/api/onboarding", openApiJson, StringComparison.Ordinal);

            var activities = capture.RecordedActivities;
            Assert.NotEmpty(activities);
            Assert.Contains(activities, a => a.TraceId != default);
        });
    }

    [Fact]
    public async Task Scenario5_ProductionSecurityHardening_FailFastGuard_AndSafeProblemDetails()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var devSecretEnv = WorkflowTestSupport.BuildHostEnvironment(
                _containers,
                databaseName,
                "Production",
                RuthHostFactory.DefaultDevJwtSigningKey
            );
            var failedDefault = await HostProcessRunner.RunHostAsync(
                devSecretEnv,
                TimeSpan.FromSeconds(15)
            );
            Assert.False(failedDefault.TimedOut);
            Assert.NotEqual(0, failedDefault.ExitCode);

            var shortSecretEnv = WorkflowTestSupport.BuildHostEnvironment(
                _containers,
                databaseName,
                "Production",
                "too-short-secret"
            );
            var failedShort = await HostProcessRunner.RunHostAsync(
                shortSecretEnv,
                TimeSpan.FromSeconds(15)
            );
            Assert.False(failedShort.TimedOut);
            Assert.NotEqual(0, failedShort.ExitCode);

            await AssertValidProductionProcessAsync(databaseName);
        });
    }

    [Fact]
    public async Task Scenario7_ConcurrentFirstRunOnboardingRace_AllowsStrictlyOneWinner()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            const int concurrency = 8;
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var tasks = new Task<(HttpStatusCode Status, string Body)>[concurrency];
            for (var i = 0; i < concurrency; i++)
            {
                var index = i;
                tasks[i] = SendConcurrentOnboardingAsync(host, index, gate.Task);
            }

            gate.SetResult();
            var results = await Task.WhenAll(tasks);

            var successes = results.Count(r =>
                r.Status is HttpStatusCode.OK or HttpStatusCode.Created
            );
            var rejected = results.Count(r =>
                r.Status is HttpStatusCode.Conflict or HttpStatusCode.Forbidden
            );

            Assert.Equal(1, successes);
            Assert.Equal(concurrency - 1, rejected);

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            Assert.Equal(1L, await probe.CountOrganizationProfilesAsync());
            Assert.Equal(1L, await probe.CountUserAccountsAsync());
        });
    }

    private static async Task AssertPostOnboardingShellAndLockAsync(HttpClient noRedirect)
    {
        using var homeResp = await noRedirect.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResp.StatusCode);
        var homeHtml = await homeResp.Content.ReadAsStringAsync();

        Assert.Contains("default-fuib", homeHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-", homeHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("theme", homeHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "An unhandled error has occurred",
            homeHtml,
            StringComparison.OrdinalIgnoreCase
        );

        await WorkflowTestSupport.AssertRedirectAsync(noRedirect, "/onboarding", "/");

        using var statusAfter = await noRedirect.GetAsync("/api/onboarding/status");
        Assert.Equal(HttpStatusCode.OK, statusAfter.StatusCode);
        var statusAfterJson = await statusAfter.Content.ReadAsStringAsync();
        Assert.Contains(
            "\"isSelfRegistrationLocked\":true",
            statusAfterJson,
            StringComparison.OrdinalIgnoreCase
        );

        using var replayContent = TestOnboardingPayloads.ToJsonContent(
            TestOnboardingPayloads.CreateValidPayload()
        );
        using var replayResp = await noRedirect.PostAsync("/api/onboarding", replayContent);
        var replayBody = await replayResp.Content.ReadAsStringAsync();

        Assert.True(replayResp.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden);
        WorkflowTestSupport.AssertProblemDetailsAndNoLeaks(replayResp, replayBody);
    }

    private async Task AssertValidProductionProcessAsync(string databaseName)
    {
        var validEnv = WorkflowTestSupport.BuildHostEnvironment(
            _containers,
            databaseName,
            "Production",
            RuthHostFactory.SafeTestJwtSigningKey
        );

        await using var runningHost = await HostProcessRunner.StartHostAsync(
            validEnv,
            TimeSpan.FromSeconds(30)
        );
        using var client = new HttpClient { BaseAddress = runningHost.BaseAddress };

        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        using var badContent = new StringContent("{ invalid", Encoding.UTF8, "application/json");
        using var errResp = await client.PostAsync("/api/onboarding", badContent);
        var errBody = await errResp.Content.ReadAsStringAsync();
        WorkflowTestSupport.AssertProblemDetailsAndNoLeaks(
            errResp,
            errBody,
            RuthHostFactory.SafeTestJwtSigningKey
        );

        using var onboardDoc = await WorkflowTestSupport.CompleteOnboardingAsync(client);
        Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));
    }

    private static async Task<(HttpStatusCode Status, string Body)> SendConcurrentOnboardingAsync(
        RuthHostFactory host,
        int index,
        Task startSignal
    )
    {
        using var client = host.CreateClient();
        var payload = TestOnboardingPayloads.CreateValidPayload(
            organizationName: string.Create(CultureInfo.InvariantCulture, $"Church {index}"),
            adminUserName: string.Create(CultureInfo.InvariantCulture, $"admin{index}"),
            adminEmail: string.Create(
                CultureInfo.InvariantCulture,
                $"admin{index}@gracechurch.test"
            )
        );
        using var content = TestOnboardingPayloads.ToJsonContent(payload);

        await startSignal;
        using var response = await client.PostAsync("/api/onboarding", content);
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, body);
    }
}
