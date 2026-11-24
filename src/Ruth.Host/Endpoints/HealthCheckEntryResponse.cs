using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ruth.Host.Endpoints;

/// <summary>Represents the readiness state of a single host dependency check.</summary>
/// <param name="Name">Registered health check name.</param>
/// <param name="Status">Current health status of the dependency.</param>
/// <param name="DurationMs">Execution duration of the check in milliseconds.</param>
/// <param name="Description">Optional safe status summary without secrets.</param>
public sealed record HealthCheckEntryResponse(
    string Name,
    HealthStatus Status,
    double DurationMs,
    string? Description
);
