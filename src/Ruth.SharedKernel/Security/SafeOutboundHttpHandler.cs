using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// SSRF-safe outbound HTTP message handler enforcing DNS resolution + IP deny-lists, internal Docker
/// hostname blocking, and cross-origin redirect rejection before payload transmission.
/// </summary>
public sealed class SafeOutboundHttpHandler : DelegatingHandler
{
    private readonly OutboundEgressOptions _options;
    private readonly Func<string, CancellationToken, ValueTask<IPAddress[]>> _dnsResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeOutboundHttpHandler" /> class backed by a hardened
    /// <see cref="SocketsHttpHandler" /> with <c lang="csharp">ConnectCallback</c> DNS-rebinding protection.
    /// </summary>
    public SafeOutboundHttpHandler(
        OutboundEgressOptions? options = null,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? dnsResolver = null
    )
    {
        _options = options ?? new OutboundEgressOptions();
        _dnsResolver = dnsResolver ?? DefaultDnsResolverAsync;
        InnerHandler = CreateSocketsHandler(_options, _dnsResolver);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeOutboundHttpHandler" /> class wrapping an explicit
    /// inner <see cref="HttpMessageHandler" /> (used by <c lang="csharp">IHttpClientFactory</c> pipelines and unit tests).
    /// </summary>
    public SafeOutboundHttpHandler(
        HttpMessageHandler innerHandler,
        OutboundEgressOptions? options = null,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? dnsResolver = null
    )
        : base(innerHandler)
    {
        ArgumentNullException.ThrowIfNull(innerHandler);
        _options = options ?? new OutboundEgressOptions();
        _dnsResolver = dnsResolver ?? DefaultDnsResolverAsync;
    }

    private SafeOutboundHttpHandler(
        OutboundEgressOptions? options,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? dnsResolver,
        bool leaveInnerUnset
    )
    {
        _ = leaveInnerUnset;
        _options = options ?? new OutboundEgressOptions();
        _dnsResolver = dnsResolver ?? DefaultDnsResolverAsync;
    }

    /// <summary>
    /// Creates a pipeline <see cref="DelegatingHandler" /> whose <see cref="DelegatingHandler.InnerHandler" /> will be
    /// attached by <c lang="csharp">IHttpClientFactory</c>.
    /// </summary>
    public static SafeOutboundHttpHandler CreatePipelineHandler(
        OutboundEgressOptions? options = null,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? dnsResolver = null
    )
    {
        return new SafeOutboundHttpHandler(options, dnsResolver, leaveInnerUnset: true);
    }

    /// <summary>
    /// Creates a hardened <see cref="SocketsHttpHandler" /> with auto-redirect disabled and a DNS-pinning
    /// <see cref="SocketsHttpHandler.ConnectCallback" /> that blocks private, loopback, and metadata IPs.
    /// </summary>
    public static SocketsHttpHandler CreateSocketsHandler(
        OutboundEgressOptions? options = null,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? dnsResolver = null
    )
    {
        var effectiveOptions = options ?? new OutboundEgressOptions();
        var effectiveResolver = dnsResolver ?? DefaultDnsResolverAsync;

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            ConnectTimeout = effectiveOptions.ConnectTimeout,
            ConnectCallback = (context, ct) =>
                ConnectValidatedSocketAsync(
                    context.DnsEndPoint,
                    effectiveOptions,
                    effectiveResolver,
                    ct
                ),
        };
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUri = request.RequestUri;
        await ValidateUriAndResolvedAddressesAsync(currentUri, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        var redirectsRemaining = Math.Max(0, _options.MaxSameOriginRedirects);

        while (IsRedirectStatusCode(response.StatusCode) && response.Headers.Location is not null)
        {
            var nextUri = ResolveRedirectUri(currentUri!, response.Headers.Location);
            EnsureAllowedRedirectTransition(
                currentUri!,
                nextUri,
                request.Method,
                response.StatusCode,
                redirectsRemaining
            );
            await ValidateUriAndResolvedAddressesAsync(nextUri, cancellationToken);

            response.Dispose();
            redirectsRemaining--;
            currentUri = nextUri;

            using var redirectRequest = new HttpRequestMessage(HttpMethod.Get, nextUri);
            CopySafeRedirectHeaders(request, redirectRequest);
            response = await base.SendAsync(redirectRequest, cancellationToken);
        }

        return response;
    }

    private async Task ValidateUriAndResolvedAddressesAsync(
        Uri? targetUri,
        CancellationToken cancellationToken
    )
    {
        OutboundAddressGuard.EnsureAllowedUri(targetUri, _options.AdditionalBlockedHostnames);
        _ = await ResolveAndValidateAddressesAsync(
            targetUri!.DnsSafeHost,
            _options,
            _dnsResolver,
            cancellationToken
        );
    }

    private void EnsureAllowedRedirectTransition(
        Uri currentUri,
        Uri nextUri,
        HttpMethod originalMethod,
        HttpStatusCode statusCode,
        int redirectsRemaining
    )
    {
        OutboundAddressGuard.EnsureAllowedUri(nextUri, _options.AdditionalBlockedHostnames);

        if (_options.BlockCrossOriginRedirects && !HasSameOrigin(currentUri, nextUri))
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.CrossOriginRedirectBlocked",
                $"Cross-origin redirect from '{currentUri.Authority}' to '{nextUri.Authority}' is blocked."
            );
        }

        if (redirectsRemaining <= 0)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.RedirectLimitExceeded",
                "Maximum permitted same-origin redirect hops exceeded."
            );
        }

        var canFollowAsGet =
            originalMethod == HttpMethod.Get
            || originalMethod == HttpMethod.Head
            || statusCode == HttpStatusCode.SeeOther;
        if (!canFollowAsGet)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.UnsafeMethodRedirectBlocked",
                $"Redirect on non-idempotent HTTP method '{originalMethod.Method}' is blocked."
            );
        }
    }

    private static async ValueTask<Stream> ConnectValidatedSocketAsync(
        DnsEndPoint endPoint,
        OutboundEgressOptions options,
        Func<string, CancellationToken, ValueTask<IPAddress[]>> dnsResolver,
        CancellationToken cancellationToken
    )
    {
        OutboundAddressGuard.EnsureAllowedHost(endPoint.Host, options.AdditionalBlockedHostnames);
        var addresses = await ResolveAndValidateAddressesAsync(
            endPoint.Host,
            options,
            dnsResolver,
            cancellationToken
        );

        Exception? lastError = null;
        foreach (var ip in addresses)
        {
            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(ip, endPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                socket.Dispose();
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Unable to connect to validated outbound host '{endPoint.Host}:{endPoint.Port}'."
            ),
            lastError
        );
    }

    private static async ValueTask<IPAddress[]> ResolveAndValidateAddressesAsync(
        string host,
        OutboundEgressOptions options,
        Func<string, CancellationToken, ValueTask<IPAddress[]>> dnsResolver,
        CancellationToken cancellationToken
    )
    {
        OutboundAddressGuard.EnsureAllowedHost(host, options.AdditionalBlockedHostnames);

        if (IPAddress.TryParse(host.Trim().TrimEnd('.'), out var literal))
        {
            OutboundAddressGuard.EnsureAllowedIpAddress(literal);
            return [literal];
        }

        var addresses = await dnsResolver(host, cancellationToken);
        if (addresses is null || addresses.Length == 0)
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.DnsResolutionEmpty",
                $"DNS resolution returned zero addresses for outbound host '{host}'."
            );
        }

        foreach (var address in addresses)
        {
            OutboundAddressGuard.EnsureAllowedIpAddress(address);
        }

        return addresses;
    }

    private static ValueTask<IPAddress[]> DefaultDnsResolverAsync(
        string host,
        CancellationToken cancellationToken
    )
    {
        return new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host, cancellationToken));
    }

    private static Uri ResolveRedirectUri(Uri currentUri, Uri location)
    {
        return location.IsAbsoluteUri ? location
            : Uri.TryCreate(currentUri, location, out var resolved) ? resolved
            : throw new OutboundSsrfBlockedException(
                "Outbound.InvalidRedirectUri",
                "Redirect Location header could not be resolved to a valid URI."
            );
    }

    private static bool HasSameOrigin(Uri left, Uri right)
    {
        return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                left.DnsSafeHost,
                right.DnsSafeHost,
                StringComparison.OrdinalIgnoreCase
            )
            && left.Port == right.Port;
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return statusCode
            is HttpStatusCode.MovedPermanently
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect;
    }

    private static void CopySafeRedirectHeaders(HttpRequestMessage source, HttpRequestMessage dest)
    {
        foreach (var header in source.Headers)
        {
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _ = dest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}
