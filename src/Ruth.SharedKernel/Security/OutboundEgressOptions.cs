namespace Ruth.SharedKernel.Security;

/// <summary>
/// Configuration options governing SSRF-safe outbound HTTP egress validation and redirect policies.
/// </summary>
public sealed class OutboundEgressOptions
{
    /// <summary>Gets whether cross-origin HTTP redirect hops (scheme, host, or port changes) are blocked.</summary>
    public bool BlockCrossOriginRedirects { get; init; } = true;

    /// <summary>Gets the maximum number of same-origin HTTP redirects permitted before aborting.</summary>
    public int MaxSameOriginRedirects { get; init; } = 3;

    /// <summary>Gets the TCP socket connection timeout applied by the primary <see cref="SocketsHttpHandler" />.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets optional additional internal hostnames or suffixes blocked from outbound egress.</summary>
    public IReadOnlySet<string> AdditionalBlockedHostnames { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
