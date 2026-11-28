using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Configuration;

/// <summary>
/// Explicit reverse-proxy trust boundaries governing <c lang="text">X-Forwarded-*</c> header acceptance.
/// </summary>
public sealed class ProxyTrustOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ProxyTrust";

    /// <summary>Gets explicit trusted reverse-proxy CIDR blocks (e.g., <c lang="text">10.10.0.0/16</c>).</summary>
    public IList<string> TrustedProxyCidrs { get; } = [];

    /// <summary>Gets explicit trusted reverse-proxy IP literals.</summary>
    public IList<string> TrustedProxyIps { get; } = [];

    /// <summary>Gets or sets the maximum number of reverse-proxy hops processed per request.</summary>
    [Range(1, 10)]
    public int ForwardLimit { get; set; } = 1;

    /// <summary>Gets or sets whether spoofed <c lang="text">X-Forwarded-*</c> headers from untrusted peers are rejected with HTTP 400.</summary>
    public bool RejectUntrustedForwardedHeaders { get; set; } = true;

    /// <summary>Gets or sets whether trusted proxies are permitted to forward <c lang="text">X-Forwarded-Host</c> (defaults to false so Host headers cannot be overridden).</summary>
    public bool AllowTrustedForwardedHost { get; set; }
}
