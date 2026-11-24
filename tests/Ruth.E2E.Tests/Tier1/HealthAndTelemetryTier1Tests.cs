using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class HealthAndTelemetryTier1Tests(E2EContainersFixture containers)
{
    [Fact]
    public async Task Feature10_HealthLive_Returns200HealthyJson()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Feature10_HealthReadiness_Returns200WhenAllDependenciesHealthy()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feature10_HealthReadiness_ResponseBodyIndicatesHealthyStatus()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature10_HealthEndpoints_AreAccessibleBeforeOnboarding()
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
    }

    [Fact]
    public async Task Feature10_HealthReadiness_CompletesWithinBoundedTimeout()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        var start = Stopwatch.GetTimestamp();
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var elapsed = Stopwatch.GetElapsedTime(start);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(elapsed < TimeSpan.FromSeconds(10), $"Health check took {elapsed}.");
    }

    [Fact]
    public async Task Feature11_ActivityCapture_RecordsAspNetCoreHttpServerActivity()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var capture = RuthHostFactory.StartActivityCapture();
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEmpty(capture.RecordedActivities);
        Assert.Contains(capture.RecordedActivities, activity =>
            activity.TraceId != default && activity.SpanId != default
        );
    }

    [Fact]
    public async Task Feature11_ActivityCapture_RecordsActivitiesDuringOnboarding()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var capture = RuthHostFactory.StartActivityCapture("Ruth", "Microsoft.AspNetCore");
        using var client = scope.Factory.CreateClient();

        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        Assert.NotEmpty(capture.RecordedActivities);
        Assert.Contains(capture.RecordedActivities, activity => activity.TraceId != default);
    }

    [Fact]
    public async Task Feature11_Telemetry_PropagatesTraceparentHeader()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var capture = RuthHostFactory.StartActivityCapture();
        using var client = scope.Factory.CreateClient();

        var traceId = ActivityTraceId.CreateRandom().ToHexString();
        var spanId = ActivitySpanId.CreateRandom().ToHexString();
        var traceparent = $"00-{traceId}-{spanId}-01";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/health/live", UriKind.Relative)
        );
        _ = request.Headers.TryAddWithoutValidation("traceparent", traceparent);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(capture.RecordedActivities, activity =>
            string.Equals(
                activity.TraceId.ToHexString(),
                traceId,
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

    [Fact]
    public async Task Feature11_Telemetry_BootsCleanlyWithoutOtlpEndpointConfigured()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty,
        };
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(
            containers,
            overrides: overrides
        );
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feature11_Telemetry_RecordsNonErrorActivityForHealthyReadinessProbe()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var capture = RuthHostFactory.StartActivityCapture();
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEmpty(capture.RecordedActivities);
        Assert.DoesNotContain(capture.RecordedActivities, activity =>
            activity.Status == ActivityStatusCode.Error
        );
    }
}
