using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ruth.Host.Endpoints;

/// <summary>Represents the aggregated host readiness probe payload.</summary>
/// <param name="Status">Aggregated readiness status across all dependency checks.</param>
/// <param name="TotalDurationMs">Total elapsed duration across all checks in milliseconds.</param>
/// <param name="Checks">Individual dependency readiness check results.</param>
public sealed record ReadinessResponse(
    HealthStatus Status,
    double TotalDurationMs,
    IReadOnlyList<HealthCheckEntryResponse> Checks
);
