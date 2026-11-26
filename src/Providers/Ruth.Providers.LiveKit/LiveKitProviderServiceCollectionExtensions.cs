using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.VirtualRooms.Contracts.Providers;

namespace Ruth.Providers.LiveKit;

/// <summary>Dependency injection registration extensions for the Apache-2.0 LiveKit WebRTC provider.</summary>
public static class LiveKitProviderServiceCollectionExtensions
{
    /// <summary>Registers <see cref="LiveKitOptions" /> and <see cref="LiveKitRoomProvider" />.</summary>
    public static IServiceCollection AddLiveKitVirtualRoomProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<LiveKitOptions>(
            configuration.GetSection(LiveKitOptions.SectionName)
        );
        _ = services.AddHttpClient(LiveKitRoomProvider.HttpClientName, static client =>
            client.Timeout = TimeSpan.FromSeconds(15)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IVirtualRoomProvider, LiveKitRoomProvider>()
        );
        return services;
    }
}
