using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;

namespace Ruth.Host.DependencyInjection;

internal static class HostForwardedHeadersExtensions
{
    public static IServiceCollection AddRuthForwardedHeaders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<ProxyTrustOptions>()
            .BindConfiguration(ProxyTrustOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        _ = services
            .AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ProxyTrustOptions>>(ConfigureForwardedHeaders);

        return services;
    }

    private static void ConfigureForwardedHeaders(
        ForwardedHeadersOptions forwardedOptions,
        IOptions<ProxyTrustOptions> proxyOptionsAccessor
    )
    {
        var proxyOptions = proxyOptionsAccessor.Value;
        forwardedOptions.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        if (proxyOptions.AllowTrustedForwardedHost)
        {
            forwardedOptions.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;
        }

        forwardedOptions.ForwardLimit = Math.Clamp(proxyOptions.ForwardLimit, 1, 10);
        forwardedOptions.RequireHeaderSymmetry = false;
        forwardedOptions.KnownIPNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();

        foreach (var rawIp in proxyOptions.TrustedProxyIps)
        {
            if (IPAddress.TryParse(rawIp?.Trim(), out var ip))
            {
                forwardedOptions.KnownProxies.Add(NormalizeIp(ip));
            }
        }

        foreach (var rawCidr in proxyOptions.TrustedProxyCidrs)
        {
            if (System.Net.IPNetwork.TryParse(rawCidr?.Trim(), out var network))
            {
                forwardedOptions.KnownIPNetworks.Add(network);
            }
        }
    }

    internal static bool IsTrustedProxy(IPAddress? remoteIp, ProxyTrustOptions options)
    {
        if (remoteIp is null)
        {
            return false;
        }

        var normalized = NormalizeIp(remoteIp);
        foreach (var rawIp in options.TrustedProxyIps)
        {
            if (IPAddress.TryParse(rawIp?.Trim(), out var ip) && NormalizeIp(ip).Equals(normalized))
            {
                return true;
            }
        }

        foreach (var rawCidr in options.TrustedProxyCidrs)
        {
            if (
                System.Net.IPNetwork.TryParse(rawCidr?.Trim(), out var network)
                && network.Contains(normalized)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static IPAddress NormalizeIp(IPAddress ip)
    {
        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }
}
