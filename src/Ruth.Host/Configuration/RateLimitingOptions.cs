using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Configuration;

/// <summary>Configuration options for dual per-IP + per-tenant and endpoint-specific API rate limiting.</summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section path for rate limiting settings.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Gets or sets the maximum permitted requests per fixed window per client IP.</summary>
    [Range(1, 100000)]
    public int PermitLimit { get; set; } = 300;

    /// <summary>Gets or sets the per-IP fixed window duration in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Gets or sets the maximum permitted requests per fixed window per tenant workspace.</summary>
    [Range(1, 500000)]
    public int TenantPermitLimit { get; set; } = 1200;

    /// <summary>Gets or sets the per-tenant fixed window duration in seconds.</summary>
    [Range(1, 3600)]
    public int TenantWindowSeconds { get; set; } = 60;

    /// <summary>Gets or sets the strict authentication / recovery brute-force limit per (tenant, client IP).</summary>
    [Range(1, 10000)]
    public int AuthPermitLimit { get; set; } = 30;

    /// <summary>Gets or sets the authentication rate-limit window in seconds.</summary>
    [Range(1, 3600)]
    public int AuthWindowSeconds { get; set; } = 60;

    /// <summary>Gets or sets the strict Check-In Kiosk &amp; RSVP lookup limit per (tenant, client IP).</summary>
    [Range(1, 10000)]
    public int KioskPermitLimit { get; set; } = 45;

    /// <summary>Gets or sets the Kiosk &amp; RSVP rate-limit window in seconds.</summary>
    [Range(1, 3600)]
    public int KioskWindowSeconds { get; set; } = 60;
}
