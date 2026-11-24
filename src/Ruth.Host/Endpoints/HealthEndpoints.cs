using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ruth.Host.Endpoints;

/// <summary>Maps host liveness and operational probe endpoints.</summary>
public static class HealthEndpoints
{
    /// <summary>Registers health probe endpoints on the application route builder.</summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapGet("/health/live", static Ok<LivenessResponse> () =>
                TypedResults.Ok(new LivenessResponse("Healthy"))
            )
            .AllowAnonymous()
            .WithName("GetLiveness")
            .WithSummary("Get host liveness status")
            .WithDescription("Returns 200 OK when the Ruth.Host process is alive.");

        _ = app.MapGet("/health", GetReadinessAsync)
            .AllowAnonymous()
            .WithName("GetReadiness")
            .WithSummary("Get host dependency readiness status")
            .WithDescription(
                "Probes PostgreSQL 18, Redis 8, and RustFS readiness with bounded timeouts."
            )
            .Produces<ReadinessResponse>(StatusCodes.Status200OK)
            .Produces<ReadinessResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<
        Results<Ok<ReadinessResponse>, JsonHttpResult<ReadinessResponse>>
    > GetReadinessAsync(HealthCheckService healthCheckService, CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(
            static registration => registration.Tags.Contains("ready"),
            cancellationToken
        );

        var checks = report
            .Entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .Select(static entry => new HealthCheckEntryResponse(
                entry.Key,
                entry.Value.Status,
                Math.Round(
                    entry.Value.Duration.TotalMilliseconds,
                    2,
                    MidpointRounding.AwayFromZero
                ),
                entry.Value.Description
            ))
            .ToArray();

        var response = new ReadinessResponse(
            report.Status,
            Math.Round(report.TotalDuration.TotalMilliseconds, 2, MidpointRounding.AwayFromZero),
            checks
        );

        return report.Status == HealthStatus.Healthy
            ? TypedResults.Ok(response)
            : TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
