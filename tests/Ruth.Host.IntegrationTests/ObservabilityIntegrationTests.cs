using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Host.Observability;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class ObservabilityIntegrationTests(HostContainersFixture containers)
{
    [Fact]
    public async Task Observability_CapturesAspNetCoreAndRuthHostActivitiesWithTraceparent()
    {
        var recorded = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source =>
                source.Name.StartsWith("Ruth", StringComparison.Ordinal)
                || source.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal),
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => recorded.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(listener);

        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            containers.RedisConnectionString,
            containers.RustFsEndpoint
        );
        using var client = factory.CreateNoRedirectClient();

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

        using (var hostActivity = HostDiagnostics.ActivitySource.StartActivity("TestHostOperation"))
        {
            Assert.NotNull(hostActivity);
        }

        Assert.Contains(recorded, activity =>
            string.Equals(
                activity.TraceId.ToHexString(),
                traceId,
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.Contains(recorded, static activity =>
            string.Equals(
                activity.Source.Name,
                HostDiagnostics.ActivitySourceName,
                StringComparison.Ordinal
            )
        );
    }

    [Fact]
    public async Task Observability_CapturesHealthCheckHistogramAndAspNetCoreMetrics()
    {
        var recordedMeters = new ConcurrentQueue<string>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (
                    instrument.Meter.Name.StartsWith("Ruth", StringComparison.Ordinal)
                    || instrument.Meter.Name.StartsWith(
                        "Microsoft.AspNetCore",
                        StringComparison.Ordinal
                    )
                )
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<double>(
            (instrument, _, _, _) => recordedMeters.Enqueue(instrument.Name)
        );
        meterListener.SetMeasurementEventCallback<long>(
            (instrument, _, _, _) => recordedMeters.Enqueue(instrument.Name)
        );
        meterListener.Start();

        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            containers.RedisConnectionString,
            containers.RustFsEndpoint
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        meterListener.RecordObservableInstruments();
        Assert.Contains("ruth.host.health_check.duration", recordedMeters, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://127.0.0.1:4317")]
    public async Task Observability_BootsCleanlyWhenOtlpEndpointIsEmptyOrUnreachable(
        string otlpEndpoint
    )
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Observability:OtlpEndpoint"] = otlpEndpoint,
            ["OpenTelemetry:OtlpEndpoint"] = otlpEndpoint,
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = otlpEndpoint,
        };

        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            overrides: overrides
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
