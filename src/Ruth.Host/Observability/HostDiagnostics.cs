using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ruth.Host.Observability;

/// <summary>OpenTelemetry instrumentation primitives for Ruth.Host.</summary>
public static class HostDiagnostics
{
    /// <summary>ActivitySource name for host-level operations.</summary>
    public const string ActivitySourceName = "Ruth.Host";

    /// <summary>Meter name for host-level metrics.</summary>
    public const string MeterName = "Ruth.Host";

    /// <summary>Shared <see cref="System.Diagnostics.ActivitySource" /> for host operations.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Shared <see cref="System.Diagnostics.Metrics.Meter" /> for host metrics.</summary>
    public static readonly Meter Meter = new(MeterName);

    /// <summary>Histogram tracking host readiness health check durations in milliseconds.</summary>
    public static readonly Histogram<double> HealthCheckDurationMs = Meter.CreateHistogram<double>(
        "ruth.host.health_check.duration",
        unit: "ms",
        description: "Duration of host readiness health check executions in milliseconds."
    );
}
