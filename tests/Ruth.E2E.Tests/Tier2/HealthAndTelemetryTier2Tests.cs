using System.Net;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class HealthAndTelemetryTier2Tests(E2EContainersFixture containers)
{
    private static readonly Dictionary<string, string?> _noAutoMigrate = new(StringComparer.Ordinal)
    {
        ["Bootstrap:AutoMigrateAndSeed"] = "false",
    };

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Health_WhenPostgresUnreachable_Returns503ServiceUnavailable()
    {
        var deadPg = Tier2HttpHelpers.BuildDeadPostgresConn(
            Tier2TestHelpers.GetUnusedLoopbackPort()
        );
        await using var factory = RuthHostFactory.CreateWithCustomEndpoints(
            deadPg,
            _containers.RedisConnectionString,
            _containers.RustFsEndpoint,
            overrides: _noAutoMigrate
        );
        await AssertHealthReturns503Async(factory);
    }

    [Fact]
    public async Task Health_WhenRedisUnreachable_Returns503ServiceUnavailable()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            var deadRedis = Tier2HttpHelpers.BuildDeadRedisConn(
                Tier2TestHelpers.GetUnusedLoopbackPort()
            );
            await using var factory = RuthHostFactory.CreateWithCustomEndpoints(
                conn,
                deadRedis,
                _containers.RustFsEndpoint
            );
            await AssertHealthReturns503Async(factory);
        });
    }

    [Fact]
    public async Task Health_WhenRustFsUnreachable_Returns503ServiceUnavailable()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            var deadRustFs = Tier2HttpHelpers.BuildLocalHttpUrl(
                Tier2TestHelpers.GetUnusedLoopbackPort()
            );
            await using var factory = RuthHostFactory.CreateWithCustomEndpoints(
                conn,
                _containers.RedisConnectionString,
                deadRustFs
            );
            await AssertHealthReturns503Async(factory);
        });
    }

    [Fact]
    public async Task HealthLive_WhenAllDependenciesUnreachable_Remains200Ok()
    {
        var deadPg = Tier2HttpHelpers.BuildDeadPostgresConn(
            Tier2TestHelpers.GetUnusedLoopbackPort(),
            timeoutSeconds: 1
        );
        var deadRedis = Tier2HttpHelpers.BuildDeadRedisConn(
            Tier2TestHelpers.GetUnusedLoopbackPort(),
            connectTimeoutMs: 500
        );
        var deadRustFs = Tier2HttpHelpers.BuildLocalHttpUrl(
            Tier2TestHelpers.GetUnusedLoopbackPort()
        );

        await using var factory = RuthHostFactory.CreateWithCustomEndpoints(
            deadPg,
            deadRedis,
            deadRustFs,
            overrides: _noAutoMigrate
        );
        using var client = factory.CreateNoRedirectClient();
        await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
    }

    [Fact]
    public async Task Health_PostMethodNotAllowed_Returns405()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var liveResp = await Tier2HttpHelpers.PostEmptyAsync(client, "/health/live");
            using var readyResp = await Tier2HttpHelpers.PostEmptyAsync(client, "/health");

            Assert.Equal(HttpStatusCode.MethodNotAllowed, liveResp.StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, readyResp.StatusCode);
        });
    }

    [Fact]
    public async Task Telemetry_UnreachableOtlpEndpoint_DoesNotCrashHostStartup()
    {
        var deadOtlp = Tier2HttpHelpers.BuildLocalHttpUrl(Tier2TestHelpers.GetUnusedLoopbackPort());
        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = deadOtlp,
            ["Observability:OtlpEndpoint"] = deadOtlp,
            ["OpenTelemetry:OtlpEndpoint"] = deadOtlp,
        };
        await AssertHostStartsLiveWithOverridesAsync(overrides);
    }

    [Fact]
    public async Task Telemetry_EmptyOtlpEndpoint_HandledGracefullyWithoutCrash()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty,
            ["Observability:OtlpEndpoint"] = string.Empty,
        };
        await AssertHostStartsLiveWithOverridesAsync(overrides);
    }

    [Fact]
    public async Task Telemetry_OnboardingRequest_DoesNotLeakPasswordInActivityTags()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            const string secret = "Unique!Password#998877";
            using var capture = RuthHostFactory.StartActivityCapture(
                "Ruth",
                "Microsoft.AspNetCore"
            );
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(
                client,
                TestOnboardingPayloads.CreateValidPayload(adminPassword: secret)
            );

            Assert.NotEmpty(capture.RecordedActivities);
            foreach (var act in capture.RecordedActivities)
            {
                Assert.DoesNotContain(secret, act.DisplayName, StringComparison.Ordinal);
                Assert.All(act.TagObjects, t =>
                    Assert.DoesNotContain(
                        secret,
                        t.Value?.ToString() ?? string.Empty,
                        StringComparison.Ordinal
                    )
                );
            }
        });
    }

    [Fact]
    public async Task Telemetry_Repeated404Requests_CompleteActivitiesWithNonNegativeDuration()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var capture = RuthHostFactory.StartActivityCapture(
                "Microsoft.AspNetCore",
                "Ruth"
            );
            for (var i = 0; i < 3; i++)
            {
                using var resp = await Tier2HttpHelpers.GetRelativeAsync(
                    client,
                    "/api/nonexistent-otel-probe"
                );
                Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
            }

            Assert.NotEmpty(capture.RecordedActivities);
            Assert.All(capture.RecordedActivities, a => Assert.True(a.Duration >= TimeSpan.Zero));
        });
    }

    [Fact]
    public async Task Telemetry_NoActiveListeners_HealthProbesExecuteCleanly()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
            using var readyResp = await Tier2HttpHelpers.GetRelativeAsync(client, "/health");
            Assert.Equal(HttpStatusCode.OK, readyResp.StatusCode);
        });
    }

    private async Task AssertHostStartsLiveWithOverridesAsync(
        IReadOnlyDictionary<string, string?> overrides
    )
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(
                _containers,
                conn,
                overrides: overrides
            );
            using var client = factory.CreateNoRedirectClient();
            await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
        });
    }

    private static async Task AssertHealthReturns503Async(RuthHostFactory factory)
    {
        using var client = factory.CreateNoRedirectClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var response = await Tier2HttpHelpers.GetRelativeAsync(client, "/health", cts.Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
