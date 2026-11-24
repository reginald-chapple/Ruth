using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Host.Configuration;

namespace Ruth.Host.Health;

/// <summary>Registers readiness health checks for PostgreSQL, Redis, and RustFS.</summary>
public static class HealthServiceCollectionExtensions
{
    /// <summary>Adds Ruth host readiness health checks and supporting clients.</summary>
    public static IServiceCollection AddRuthHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<HealthChecksOptions>()
            .BindConfiguration(HealthChecksOptions.SectionName)
            .ValidateDataAnnotations();

        _ = services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName);

        _ = services
            .AddOptions<RustFsOptions>()
            .BindConfiguration(RustFsOptions.SectionName)
            .ValidateDataAnnotations();

        _ = services.AddHttpClient(RustFsHealthCheck.HttpClientName);
        services.TryAddSingleton<RedisConnectionAccessor>();

        _ = services
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<RustFsHealthCheck>("rustfs", tags: ["ready"]);

        return services;
    }
}
