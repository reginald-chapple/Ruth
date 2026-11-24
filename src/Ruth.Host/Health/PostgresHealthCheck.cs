using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Ruth.Host.Configuration;
using Ruth.Host.Observability;

namespace Ruth.Host.Health;

internal sealed class PostgresHealthCheck(
    IConfiguration configuration,
    IOptions<HealthChecksOptions> options,
    ILogger<PostgresHealthCheck> logger
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var connectionString = configuration.GetConnectionString("RuthDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection string is not configured.");
        }

        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var timeoutSeconds = Math.Clamp(options.Value.TimeoutSeconds, 1, 60);
            var csBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = timeoutSeconds,
                CommandTimeout = timeoutSeconds,
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            await using var connection = new NpgsqlConnection(csBuilder.ConnectionString);
            await connection.OpenAsync(cts.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = timeoutSeconds;
            _ = await command.ExecuteScalarAsync(cts.Token);

            RecordMetric("postgresql", "healthy", startTimestamp);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RecordMetric("postgresql", "unhealthy", startTimestamp);
            HostLog.HealthCheckFailed(logger, ex, "postgresql");
            return HealthCheckResult.Unhealthy("PostgreSQL readiness check failed.");
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
