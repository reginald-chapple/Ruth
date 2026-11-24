using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;

namespace Ruth.Host.DependencyInjection;

internal static class HostConfigurationExtensions
{
    public static IServiceCollection AddRuthJwtConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .PostConfigure<IConfiguration>(
                static (options, configuration) =>
                    options.SigningKey = JwtOptions.ResolveSigningKey(configuration)
            )
            .ValidateOnStart();

        _ = services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        return services;
    }
}
