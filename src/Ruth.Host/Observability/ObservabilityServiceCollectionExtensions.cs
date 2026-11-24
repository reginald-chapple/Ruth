using System.Reflection;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ruth.Host.Configuration;

namespace Ruth.Host.Observability;

/// <summary>Registers OpenTelemetry logging, metrics, and distributed tracing for Ruth.Host.</summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>Configures OpenTelemetry tracing, metrics, and logging on the host builder.</summary>
    public static WebApplicationBuilder AddRuthObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder
            .Services.AddOptions<ObservabilityOptions>()
            .BindConfiguration(ObservabilityOptions.SectionName);

        var serviceName = builder.Configuration["Observability:ServiceName"];
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName = HostDiagnostics.ActivitySourceName;
        }

        var serviceVersion = typeof(ObservabilityServiceCollectionExtensions)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "1.0.0";

        var otlpUri = TryResolveOtlpUri(builder.Configuration);

        _ = builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            )
            .WithTracing(tracing => ConfigureTracing(tracing, otlpUri))
            .WithMetrics(metrics => ConfigureMetrics(metrics, otlpUri))
            .WithLogging(
                logging => ConfigureLogging(logging, otlpUri),
                static options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                }
            );

        return builder;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, Uri? otlpUri)
    {
        _ = tracing
            .AddSource("Ruth.*")
            .AddSource(HostDiagnostics.ActivitySourceName)
            .AddAspNetCoreInstrumentation(static options =>
            {
                options.RecordException = false;
            })
            .AddHttpClientInstrumentation()
            .AddNpgsql();

        if (otlpUri is not null)
        {
            _ = tracing.AddOtlpExporter(options => options.Endpoint = otlpUri);
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, Uri? otlpUri)
    {
        _ = metrics
            .AddMeter("Ruth.*")
            .AddMeter(HostDiagnostics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddNpgsqlInstrumentation();

        if (otlpUri is not null)
        {
            _ = metrics.AddOtlpExporter(options => options.Endpoint = otlpUri);
        }
    }

    private static void ConfigureLogging(LoggerProviderBuilder logging, Uri? otlpUri)
    {
        if (otlpUri is not null)
        {
            _ = logging.AddOtlpExporter(options => options.Endpoint = otlpUri);
        }
    }

    private static Uri? TryResolveOtlpUri(IConfiguration configuration)
    {
        var rawEndpoint = ObservabilityOptions.ResolveOtlpEndpoint(configuration);
        return
            !string.IsNullOrWhiteSpace(rawEndpoint)
            && Uri.TryCreate(rawEndpoint, UriKind.Absolute, out var uri)
            && (
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            )
            ? uri
            : null;
    }
}
