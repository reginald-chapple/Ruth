using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.Observability;

namespace Ruth.Host.Health;

internal sealed class RedisHealthCheck(
    RedisConnectionAccessor connectionAccessor,
    IOptions<HealthChecksOptions> options,
    ILogger<RedisHealthCheck> logger
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var timeoutSeconds = Math.Clamp(options.Value.TimeoutSeconds, 1, 60);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            _ = await connectionAccessor.PingAsync(timeoutSeconds, cts.Token);

            RecordMetric("redis", "healthy", startTimestamp);
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RecordMetric("redis", "unhealthy", startTimestamp);
            HostLog.HealthCheckFailed(logger, ex, "redis");
            return HealthCheckResult.Unhealthy("Redis readiness check failed.");
        }
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
