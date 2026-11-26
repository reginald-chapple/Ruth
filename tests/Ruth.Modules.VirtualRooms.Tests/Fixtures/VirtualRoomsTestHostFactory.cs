using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Crm;
using Ruth.Modules.Groups;
using Ruth.Modules.Identity;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.CommunicationTransports;
using Ruth.Providers.LiveKit;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.VirtualRooms.Tests.Fixtures;

internal static class VirtualRoomsTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        VirtualRoomsRecordingStorageSpy? storageSpy = null,
        Action<IServiceCollection>? configure = null
    )
    {
        var configuration = BuildConfiguration(connectionString);
        var services = new ServiceCollection();
        RegisterModules(services, configuration, storageSpy);
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

    private static IConfigurationRoot BuildConfiguration(string connectionString)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "true",
                    ["VirtualRooms:ActiveProvider"] = "in-memory",
                    ["VirtualRooms:LiveKit:ApiKey"] = "devkey",
                    ["VirtualRooms:LiveKit:ApiSecret"] =
                        "ruth-inmemory-livekit-dev-signing-secret-32chars!",
                    ["Jwt:Issuer"] = "ruth-test",
                    ["Jwt:Audience"] = "ruth-test",
                    ["Jwt:SigningKey"] =
                        "ruth-test-identity-signing-key-at-least-32-characters-long!",
                }
            )
            .Build();
    }

    private static void RegisterModules(
        IServiceCollection services,
        IConfiguration configuration,
        VirtualRoomsRecordingStorageSpy? storageSpy
    )
    {
        _ = services.AddLogging();
        _ = services.AddSingleton(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddIdentityModule(configuration);
        _ = services.AddCoreModule(configuration);
        _ = services.AddSingleton<IStorageProvider>(
            storageSpy ?? new VirtualRoomsRecordingStorageSpy()
        );
        _ = services.AddCrmModule(configuration);
        _ = services.AddGroupsModule(configuration);
        _ = services.AddCommunicationModule(configuration);
        _ = services.AddCommunicationTransportProviders(configuration);
        _ = services.AddLiveKitVirtualRoomProvider(configuration);
        _ = services.AddVirtualRoomsModule(configuration);
    }
}
