using Microsoft.Extensions.Configuration;
using Ruth.Modules.VirtualRooms.Contracts.Providers;

namespace Ruth.Modules.VirtualRooms.Infrastructure;

internal sealed class VirtualRoomProviderSelector(
    IEnumerable<IVirtualRoomProvider> providers,
    IConfiguration configuration
)
{
    public IVirtualRoomProvider ResolveActive()
    {
        var explicitKey = configuration["VirtualRooms:ActiveProvider"];
        var liveKitEnabled = configuration.GetValue<bool>("VirtualRooms:LiveKit:Enabled");
        var desiredKey =
            !string.IsNullOrWhiteSpace(explicitKey) ? explicitKey.Trim()
            : liveKitEnabled ? "livekit"
            : InMemoryVirtualRoomProvider.Key;

        return providers.FirstOrDefault(p =>
            string.Equals(p.ProviderKey, desiredKey, StringComparison.OrdinalIgnoreCase)
        ) ?? providers.First();
    }

    public string DefaultStorageBucket =>
        configuration["Storage:RustFs:BucketName"] is { Length: > 0 } bucket ? bucket : "ruth";
}
