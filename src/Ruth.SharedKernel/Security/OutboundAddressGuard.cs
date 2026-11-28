using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Pure-BCL SSRF validator enforcing scheme, authority, internal Docker/service hostname, and IPv4/IPv6 deny-lists.
/// </summary>
public static class OutboundAddressGuard
{
    private static readonly FrozenSet<string> _blockedExactHosts = new[]
    {
        "localhost",
        "localhost.localdomain",
        "ip6-localhost",
        "ip6-loopback",
        "host.docker.internal",
        "gateway.docker.internal",
        "kubernetes",
        "kubernetes.default",
        "kubernetes.default.svc",
        "metadata",
        "metadata.google.internal",
        "instance-data",
        "postgres",
        "postgresql",
        "redis",
        "rustfs",
        "minio",
        "mailhog",
        "otel-collector",
        "jaeger",
        "seq",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] _blockedHostSuffixes =
    [
        ".localhost",
        ".local",
        ".internal",
        ".docker",
        ".svc",
        ".cluster.local",
        ".home.arpa",
    ];

    /// <summary>Validates that <paramref name="uri" /> is an absolute HTTP/HTTPS URI with no userinfo and an allowed hostname.</summary>
    public static void EnsureAllowedUri(
        Uri? uri,
        IReadOnlySet<string>? additionalBlockedHosts = null
    )
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.InvalidUri",
                "Outbound HTTP target must be an absolute URI."
            );
        }

        var isHttp =
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        if (!isHttp)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.SchemeNotAllowed",
                $"Outbound URI scheme '{uri.Scheme}' is not permitted; only http and https are allowed."
            );
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.UserInfoNotAllowed",
                "Outbound URI must not contain embedded userinfo credentials."
            );
        }

        if (uri.Port is <= 0 or > 65535)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.InvalidPort",
                "Outbound URI port is out of range."
            );
        }

        EnsureAllowedHost(uri.DnsSafeHost, additionalBlockedHosts);
    }

    /// <summary>Validates that <paramref name="host" /> is not an internal service hostname or disallowed IP literal.</summary>
    public static void EnsureAllowedHost(
        string? host,
        IReadOnlySet<string>? additionalBlockedHosts = null
    )
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.EmptyHost",
                "Outbound target hostname is empty."
            );
        }

        var normalized = host.Trim().TrimEnd('.');
        if (normalized.Length == 0)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.EmptyHost",
                "Outbound target hostname is empty."
            );
        }

        if (IPAddress.TryParse(normalized, out var ipLiteral))
        {
            EnsureAllowedIpAddress(ipLiteral);
            return;
        }

        if (
            _blockedExactHosts.Contains(normalized)
            || (additionalBlockedHosts?.Contains(normalized) == true)
            || !normalized.Contains('.', StringComparison.Ordinal)
            || HasBlockedSuffix(normalized)
        )
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.InternalHostnameBlocked",
                $"Outbound requests to internal or non-public host '{normalized}' are blocked."
            );
        }
    }

    /// <summary>Throws <see cref="OutboundSsrfBlockedException" /> when <paramref name="address" /> is private, loopback, link-local, metadata, or reserved.</summary>
    public static void EnsureAllowedIpAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (IsDisallowedIpAddress(address))
        {
            var addressText = address.ToString();
            throw new OutboundSsrfBlockedException(
                "Outbound.PrivateOrReservedIpBlocked",
                $"Outbound connection to non-public or metadata IP address '{addressText}' is blocked."
            );
        }
    }

    /// <summary>Returns <see langword="true" /> when <paramref name="address" /> falls in any loopback, private, link-local, metadata, unique-local, or reserved range.</summary>
    public static bool IsDisallowedIpAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            Span<byte> v4 = stackalloc byte[4];
            return !address.TryWriteBytes(v4, out _) || IsDisallowedIPv4Bytes(v4);
        }

        return address.AddressFamily != AddressFamily.InterNetworkV6 || IsDisallowedIPv6(address);
    }

    private static bool HasBlockedSuffix(string normalizedHost)
    {
        foreach (var suffix in _blockedHostSuffixes)
        {
            if (normalizedHost.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDisallowedIPv4Bytes(ReadOnlySpan<byte> b)
    {
        var first = b[0];
        var second = b[1];

        return first is 0 or 10 or 127
            || (first == 100 && (second & 0xC0) == 64)
            || (first == 169 && second == 254)
            || (first == 172 && second is >= 16 and <= 31)
            || (first == 192 && second == 0 && b[2] == 0)
            || (first == 192 && second == 168)
            || (first == 198 && second is 18 or 19)
            || first >= 224;
    }

    private static bool IsDisallowedIPv6(IPAddress ipv6)
    {
        if (
            IPAddress.IsLoopback(ipv6)
            || ipv6.Equals(IPAddress.IPv6Any)
            || ipv6.Equals(IPAddress.IPv6None)
            || ipv6.IsIPv6LinkLocal
            || ipv6.IsIPv6SiteLocal
            || ipv6.IsIPv6Multicast
        )
        {
            return true;
        }

        if (ipv6.IsIPv4MappedToIPv6)
        {
            return IsDisallowedIpAddress(ipv6.MapToIPv4());
        }

        Span<byte> bytes = stackalloc byte[16];
        if (!ipv6.TryWriteBytes(bytes, out _))
        {
            return true;
        }

        // fc00::/7 Unique Local (RFC 4193, includes AWS IPv6 IMDS fd00:ec2::254) + fec0::/10 site-local
        return (bytes[0] & 0xFE) == 0xFC
            || (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0xC0)
            || HasDisallowedEmbeddedIPv4(bytes);
    }

    private static bool HasDisallowedEmbeddedIPv4(ReadOnlySpan<byte> bytes)
    {
        // 6to4 (2002::/16) embeds IPv4 in bytes 2..5
        if (bytes[0] == 0x20 && bytes[1] == 0x02)
        {
            return IsDisallowedIPv4Bytes(bytes.Slice(2, 4));
        }

        // NAT64 well-known prefix (64:ff9b::/96) or IPv4-compatible (::w.x.y.z) embeds IPv4 in bytes 12..15
        var isNat64 =
            bytes[0] == 0x00
            && bytes[1] == 0x64
            && bytes[2] == 0xFF
            && bytes[3] == 0x9B
            && bytes[4..12].IndexOfAnyExcept((byte)0) < 0;
        var isV4Compatible = bytes[..12].IndexOfAnyExcept((byte)0) < 0;

        return (isNat64 || isV4Compatible) && IsDisallowedIPv4Bytes(bytes.Slice(12, 4));
    }
}
