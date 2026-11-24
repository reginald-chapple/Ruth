using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.Observability;

namespace Ruth.Host.Health;

internal sealed class RustFsHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IOptions<RustFsOptions> rustFsOptions,
    IOptions<HealthChecksOptions> healthOptions,
    ILogger<RustFsHealthCheck> logger
) : IHealthCheck
{
    internal const string HttpClientName = "rustfs-health";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var endpoint = RustFsOptions.ResolveEndpoint(configuration);
        if (
            string.IsNullOrWhiteSpace(endpoint)
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var baseUri)
        )
        {
            return HealthCheckResult.Unhealthy("RustFS endpoint is not configured.");
        }

        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var timeoutSeconds = ResolveTimeoutSeconds();
            return await SendHealthProbeAsync(
                baseUri,
                timeoutSeconds,
                startTimestamp,
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RecordMetric("rustfs", "unhealthy", startTimestamp);
            HostLog.HealthCheckFailed(logger, ex, "rustfs");
            return HealthCheckResult.Unhealthy("RustFS readiness check failed.");
        }
    }

    private int ResolveTimeoutSeconds()
    {
        return Math.Clamp(
            Math.Min(
                rustFsOptions.Value.HealthCheckTimeoutSeconds,
                healthOptions.Value.TimeoutSeconds
            ),
            1,
            60
        );
    }

    private async Task<HealthCheckResult> SendHealthProbeAsync(
        Uri baseUri,
        int timeoutSeconds,
        long startTimestamp,
        CancellationToken cancellationToken
    )
    {
        var probePath = string.IsNullOrWhiteSpace(rustFsOptions.Value.HealthCheckPath)
            ? "/health"
            : rustFsOptions.Value.HealthCheckPath;
        var probeUri = new Uri(baseUri, probePath);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Get, probeUri);
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token
        );

        if (response.IsSuccessStatusCode)
        {
            RecordMetric("rustfs", "healthy", startTimestamp);
            return HealthCheckResult.Healthy("RustFS is reachable.");
        }

        RecordMetric("rustfs", "unhealthy", startTimestamp);
        HostLog.HealthCheckFailed(logger, exception: null, "rustfs");
        return HealthCheckResult.Unhealthy(
            string.Create(
                CultureInfo.InvariantCulture,
                $"RustFS health probe returned HTTP {(int)response.StatusCode}."
            )
        );
    }

    private static void RecordMetric(string checkName, string status, long startTimestamp)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        HostDiagnostics.HealthCheckDurationMs.Record(
            elapsedMs,
            new KeyValuePair<string, object?>("check", checkName),
            new KeyValuePair<string, object?>("status", status)
        );
    }
}
