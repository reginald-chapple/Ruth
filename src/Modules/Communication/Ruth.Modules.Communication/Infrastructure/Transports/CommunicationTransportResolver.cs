using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Selects the active configured <see cref="ICommunicationTransport" /> for each channel with safe Dev Sink fallback.</summary>
public sealed class CommunicationTransportResolver(
    IEnumerable<ICommunicationTransport> transports,
    IOptions<CommunicationTransportOptions> options
)
{
    private readonly ICommunicationTransport[] _transports = [.. transports];
    private readonly CommunicationTransportOptions _options = options.Value;

    public IReadOnlyList<ICommunicationTransport> GetAllTransports()
    {
        return _transports;
    }

    public ICommunicationTransport ResolveForChannel(CommunicationChannel channel)
    {
        var preferredKey = GetPreferredKey(channel);
        var preferred = _transports.FirstOrDefault(t =>
            t.Channel == channel
            && t.IsConfigured
            && string.Equals(t.TransportKey, preferredKey, StringComparison.OrdinalIgnoreCase)
        );
        if (preferred is not null)
        {
            return preferred;
        }

        var liveConfigured = _transports.FirstOrDefault(t =>
            t.Channel == channel && t.IsConfigured && !IsFallbackTransport(t.TransportKey)
        );
        return liveConfigured ?? _transports.First(t => t.Channel == channel && t.IsConfigured);
    }

    private string GetPreferredKey(CommunicationChannel channel)
    {
        return channel switch
        {
            CommunicationChannel.Email => _options.PreferredEmailTransport,
            CommunicationChannel.Sms => _options.PreferredSmsTransport,
            CommunicationChannel.Push => _options.PreferredPushTransport,
            _ => DevInMemoryEmailTransport.Key,
        };
    }

    private static bool IsFallbackTransport(string key)
    {
        return string.Equals(key, DevInMemoryEmailTransport.Key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, DevInMemorySmsTransport.Key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                key,
                PushNotificationStubTransport.Key,
                StringComparison.OrdinalIgnoreCase
            );
    }
}
