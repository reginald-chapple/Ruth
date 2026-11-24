using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Configuration;

/// <summary>Configuration options for host readiness health checks.</summary>
public sealed class HealthChecksOptions
{
    /// <summary>Configuration section path for health check settings.</summary>
    public const string SectionName = "HealthChecks";

    /// <summary>Gets or sets the bounded per-check timeout in seconds.</summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 3;
}
