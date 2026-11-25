using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Providers.RustFsStorage;

/// <summary>DI registration extensions for the modular RustFS object storage provider.</summary>
public static class RustFsStorageServiceCollectionExtensions
{
    /// <summary>Registers <see cref="RustFsStorageProvider" /> as an <see cref="IStorageProvider" />.</summary>
    public static IServiceCollection AddRustFsStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptions<RustFsStorageOptions>()
            .Bind(configuration.GetSection(RustFsStorageOptions.SectionName))
            .PostConfigure(opts =>
            {
                var fallbackEndpoint = configuration["Storage:RustFs:Endpoint"]
                    ?? configuration["Storage:Endpoint"]
                    ?? configuration["Storage:ServiceUrl"]
                    ?? configuration["RustFs:Endpoint"];
                if (!string.IsNullOrWhiteSpace(fallbackEndpoint))
                {
                    opts.Endpoint = fallbackEndpoint;
                }

                var fallbackBucket = configuration["Storage:RustFs:BucketName"]
                    ?? configuration["Storage:BucketName"];
                if (!string.IsNullOrWhiteSpace(fallbackBucket))
                {
                    opts.BucketName = fallbackBucket;
                }

                if (string.IsNullOrWhiteSpace(opts.AccessKey))
                {
                    opts.AccessKey = "rustfsadmin";
                }

                if (string.IsNullOrWhiteSpace(opts.SecretKey))
                {
                    opts.SecretKey = "rustfsadmin";
                }
            });

        _ = services.AddHttpClient(RustFsStorageProvider.HttpClientName);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStorageProvider, RustFsStorageProvider>()
        );
        return services;
    }
}
