using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Configuration;

/// <summary>Configuration options for host API rate limiting.</summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section path for rate limiting settings.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Gets or sets the maximum permitted requests per fixed window.</summary>
    [Range(1, 100000)]
    public int PermitLimit { get; set; } = 300;

    /// <summary>Gets or sets the fixed window duration in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
