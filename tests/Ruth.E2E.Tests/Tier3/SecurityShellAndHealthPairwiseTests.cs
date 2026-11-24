using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier3;

[Collection(E2ETestCollection.Name)]
public sealed class SecurityShellAndHealthPairwiseTests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task ProductionSigningKeyGuard_ValidSecretBoots_AndEnforcesOnboardingGate()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(
                _containers,
                databaseName,
                environment: "Production",
                jwtSigningKey: RuthHostFactory.SafeTestJwtSigningKey
            );
            using var noRedirectClient = host.CreateNoRedirectClient();

            using var live = await noRedirectClient.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, live.StatusCode);

            using var health = await noRedirectClient.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);

            await PairwiseTestSupport.AssertRedirectAsync(noRedirectClient, "/", "/onboarding");

            using var onboarding = await noRedirectClient.GetAsync("/onboarding");
            Assert.Equal(HttpStatusCode.OK, onboarding.StatusCode);
        });
    }

    [Fact]
    public async Task ProductionEnvironment_ApiExceptionHandler_SuppressesStackTraces()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(
                _containers,
                databaseName,
                environment: "Production",
                jwtSigningKey: RuthHostFactory.SafeTestJwtSigningKey
            );
            using var client = host.CreateClient();

            using var malformed = new StringContent(
                "{ invalid-json",
                Encoding.UTF8,
                "application/json"
            );
            using var badJsonResp = await client.PostAsync("/api/onboarding", malformed);
            var badJsonBody = await badJsonResp.Content.ReadAsStringAsync();

            PairwiseTestSupport.AssertProblemDetailsResponse(badJsonResp, badJsonBody);
            PairwiseTestSupport.AssertNoLeakageInErrorBody(
                badJsonBody,
                RuthHostFactory.SafeTestJwtSigningKey
            );

            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(client);
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            using var replayContent = TestOnboardingPayloads.ToJsonContent(
                TestOnboardingPayloads.CreateValidPayload()
            );
            using var replayResp = await client.PostAsync("/api/onboarding", replayContent);
            var replayBody = await replayResp.Content.ReadAsStringAsync();

            Assert.True(
                replayResp.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden
            );
            PairwiseTestSupport.AssertProblemDetailsResponse(replayResp, replayBody);
            PairwiseTestSupport.AssertNoLeakageInErrorBody(
                replayBody,
                RuthHostFactory.SafeTestJwtSigningKey
            );
        });
    }

    [Fact]
    public async Task FluentUiShell_ThemeToggle_AndMainLayout_RenderPostOnboarding()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(client);
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            using var homeResponse = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

            var html = await homeResponse.Content.ReadAsStringAsync();
            Assert.Contains("default-fuib", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("blazor.web", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("fluent-", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("theme", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        });
    }

    [Fact]
    public async Task OpenApiDocument_ExposesOnboardingContract_AndProblemDetailsShapes()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(
                _containers,
                databaseName,
                environment: "Development"
            );
            using var client = host.CreateClient();

            using var openApiResponse = await client.GetAsync("/openapi/v1.json");
            Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);

            var json = await openApiResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("openapi", out _));
            Assert.True(root.TryGetProperty("paths", out var paths));
            Assert.True(
                paths.TryGetProperty("/api/onboarding", out _),
                $"Expected '/api/onboarding' path in OpenAPI doc: {json}"
            );

            Assert.Contains("organizationName", json, StringComparison.Ordinal);
            Assert.Contains("adminEmail", json, StringComparison.Ordinal);
            Assert.Contains("ProblemDetails", json, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HealthChecks_RustFsOrRedisDown_DegradesReadiness_WhileLivenessStays200()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var closedPort = PairwiseTestSupport.GetUnusedTcpPort();
            var unreachableRustFs = string.Create(
                CultureInfo.InvariantCulture,
                $"http://127.0.0.1:{closedPort}"
            );

            await using var host = RuthHostFactory.CreateWithCustomEndpoints(
                _containers.GetConnectionString(databaseName),
                _containers.RedisConnectionString,
                unreachableRustFs,
                jwtSigningKey: RuthHostFactory.SafeTestJwtSigningKey
            );
            using var client = host.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var liveResponse = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);

            using var healthResponse = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
        });
    }

    [Fact]
    public async Task OutOfProcess_ProductionHost_WithValidSecret_ServesHealthAndOnboardingGate()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var env = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication__Jwt__SigningKey"] = RuthHostFactory.SafeTestJwtSigningKey,
                ["ConnectionStrings__RuthDatabase"] = _containers.GetConnectionString(databaseName),
                ["ConnectionStrings__Redis"] = _containers.RedisConnectionString,
                ["ConnectionStrings__RustFs"] = _containers.RustFsEndpoint,
                ["Storage__RustFs__Endpoint"] = _containers.RustFsEndpoint,
                ["Storage__RustFs__BucketName"] = "ruth-e2e",
                ["Storage__RustFs__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
                ["Storage__RustFs__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
                ["Storage__RustFs__HealthCheckTimeoutSeconds"] = "2",
                ["HealthChecks__TimeoutSeconds"] = "2",
                ["Storage__Endpoint"] = _containers.RustFsEndpoint,
                ["Storage__ServiceUrl"] = _containers.RustFsEndpoint,
                ["Storage__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
                ["Storage__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
                ["RustFs__Endpoint"] = _containers.RustFsEndpoint,
                ["RustFs__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
                ["RustFs__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
                ["Bootstrap__AutoMigrateAndSeed"] = "true",
            };

            await using var runningHost = await HostProcessRunner.StartHostAsync(
                env,
                TimeSpan.FromSeconds(30)
            );
            using var noRedirect = runningHost.CreateNoRedirectClient();

            using var health = await noRedirect.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);

            await PairwiseTestSupport.AssertRedirectAsync(noRedirect, "/", "/onboarding");
        });
    }
}
