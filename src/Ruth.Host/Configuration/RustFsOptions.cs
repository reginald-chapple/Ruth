using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Configuration;

/// <summary>Configuration options for RustFS S3-compatible object storage.</summary>
public sealed class RustFsOptions
{
    /// <summary>Configuration section path for RustFS settings.</summary>
    public const string SectionName = "Storage:RustFs";

    private static readonly string[] _endpointPriorityKeys =
    [
        "Storage:RustFs:Endpoint",
        "Storage:Endpoint",
        "Storage:ServiceUrl",
        "RustFs:Endpoint",
        "ConnectionStrings:RustFs",
    ];

    /// <summary>Gets or sets the RustFS HTTP endpoint.</summary>
    public string? Endpoint { get; set; } = "http://localhost:9000";

    /// <summary>Gets or sets the target S3 bucket name.</summary>
    [Required]
    public string BucketName { get; set; } = "ruth";

    /// <summary>Gets or sets the RustFS access key.</summary>
    public string? AccessKey { get; set; }

    /// <summary>Gets or sets the RustFS secret key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Gets or sets the relative health probe path on the RustFS endpoint.</summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>Gets or sets the bounded timeout in seconds for RustFS health probes.</summary>
    [Range(1, 60)]
    public int HealthCheckTimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// Resolves the active RustFS endpoint from configuration providers by priority,
    /// supporting both Storage:RustFs:Endpoint and fallback keys.
    /// </summary>
    public static string? ResolveEndpoint(IConfiguration configuration)
    {
        return ConfigurationValueResolver.ResolveFirstNonEmpty(
            configuration,
            _endpointPriorityKeys
        );
    }
}
