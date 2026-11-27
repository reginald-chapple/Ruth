using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Tenancy;

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
        if (AmbientTenantContext.Instance.HasTenant && !AmbientTenantContext.Instance.IsLive)
        {
            var sandboxFallback = FindFallbackTransport(channel);
            if (sandboxFallback is not null)
            {
                return sandboxFallback;
            }
        }

        return FindPreferredTransport(channel)
            ?? FindLiveConfiguredTransport(channel)
            ?? FindFirstConfiguredTransport(channel);
    }

    private ICommunicationTransport? FindFallbackTransport(CommunicationChannel channel)
    {
        foreach (var transport in _transports)
        {
            if (
                transport.Channel == channel
                && transport.IsConfigured
                && IsFallbackTransport(transport.TransportKey)
            )
            {
                return transport;
            }
        }

        return null;
    }

    private ICommunicationTransport? FindPreferredTransport(CommunicationChannel channel)
    {
        var preferredKey = GetPreferredKey(channel);
        foreach (var transport in _transports)
        {
            if (
                transport.Channel == channel
                && transport.IsConfigured
                && string.Equals(
                    transport.TransportKey,
                    preferredKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return transport;
            }
        }

        return null;
    }

    private ICommunicationTransport? FindLiveConfiguredTransport(CommunicationChannel channel)
    {
        foreach (var transport in _transports)
        {
            if (
                transport.Channel == channel
                && transport.IsConfigured
                && !IsFallbackTransport(transport.TransportKey)
            )
            {
                return transport;
            }
        }

        return null;
    }

    private ICommunicationTransport FindFirstConfiguredTransport(CommunicationChannel channel)
    {
        foreach (var transport in _transports)
        {
            if (transport.Channel == channel && transport.IsConfigured)
            {
                return transport;
            }
        }

        throw new InvalidOperationException(
            $"No configured communication transport was registered for channel '{channel}'."
        );
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
