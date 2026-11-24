namespace Ruth.Host.Configuration;

/// <summary>Configuration options for OpenTelemetry observability.</summary>
public sealed class ObservabilityOptions
{
    /// <summary>Configuration section path for observability settings.</summary>
    public const string SectionName = "Observability";

    private static readonly string[] _otlpEndpointKeys =
    [
        "Observability:OtlpEndpoint",
        "OpenTelemetry:OtlpEndpoint",
        "OTEL_EXPORTER_OTLP_ENDPOINT",
    ];

    /// <summary>Gets or sets the logical service name reported in OpenTelemetry resources.</summary>
    public string ServiceName { get; set; } = "Ruth.Host";

    /// <summary>Gets or sets the optional OTLP collector endpoint URI.</summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Resolves the configured OTLP endpoint across Observability:OtlpEndpoint,
    /// OpenTelemetry:OtlpEndpoint, and OTEL_EXPORTER_OTLP_ENDPOINT.
    /// </summary>
    public static string? ResolveOtlpEndpoint(IConfiguration configuration)
    {
        return ConfigurationValueResolver.ResolveFirstNonEmpty(configuration, _otlpEndpointKeys);
    }
}
