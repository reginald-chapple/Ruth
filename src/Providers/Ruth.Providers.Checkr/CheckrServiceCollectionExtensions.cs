using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;

namespace Ruth.Providers.Checkr;

/// <summary>DI registration extensions for the modular Checkr background check provider.</summary>
public static class CheckrServiceCollectionExtensions
{
    /// <summary>Registers <see cref="CheckrBackgroundCheckProvider" /> as an <see cref="IBackgroundCheckProvider" />.</summary>
    public static IServiceCollection AddCheckrBackgroundCheckProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<CheckrOptions>(configuration.GetSection(CheckrOptions.SectionName));
        _ = services.AddHttpClient<CheckrBackgroundCheckProvider>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IBackgroundCheckProvider, CheckrBackgroundCheckProvider>(
                static sp => sp.GetRequiredService<CheckrBackgroundCheckProvider>()
            )
        );
        return services;
    }
}
