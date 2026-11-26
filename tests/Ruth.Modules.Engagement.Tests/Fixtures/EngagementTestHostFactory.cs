using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Crm;
using Ruth.Modules.Groups;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.CommunicationTransports;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Engagement.Tests.Fixtures;

internal static class EngagementTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        Action<IServiceCollection>? configure = null
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "true",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCoreModule(configuration);
        _ = services.AddSingleton<IStorageProvider>(new EngagementInMemoryStorageProvider());
        _ = services.AddCrmModule(configuration);
        _ = services.AddGroupsModule(configuration);
        _ = services.AddCommunicationModule(configuration);
        _ = services.AddCommunicationTransportProviders(configuration);
        _ = services.AddEngagementModule(configuration);
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            if (hosted is not BackgroundService)
            {
                await hosted.StartAsync(CancellationToken.None);
            }
        }

        return provider;
    }
}
