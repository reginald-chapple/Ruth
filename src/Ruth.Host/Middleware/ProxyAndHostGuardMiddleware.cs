using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.DependencyInjection;

namespace Ruth.Host.Middleware;

/// <summary>
/// Enforces RFC 9110 <c lang="text">Host</c> header syntax and explicit reverse-proxy trust boundaries
/// before <c lang="text">ForwardedHeadersMiddleware</c>, rate limiting, or tenant host resolution execute.
/// </summary>
internal sealed class ProxyAndHostGuardMiddleware(RequestDelegate next)
{
    private static readonly string[] _hostOverrideHeaders =
    [
        "X-Forwarded-Host",
        "X-Host",
        "X-Forwarded-Server",
        "X-Original-Host",
        "X-Original-URL",
        "X-Rewrite-URL",
    ];

    private static readonly string[] _forwardedClientHeaders =
    [
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Real-IP",
        "X-Client-IP",
        "Forwarded",
    ];

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(
        HttpContext context,
        IOptionsMonitor<ProxyTrustOptions> proxyOptionsMonitor
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(proxyOptionsMonitor);

        if (!IsValidHostHeader(context.Request.Headers))
        {
            await RejectWithProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid Host Header",
                "The HTTP Host header is missing, duplicated, or contains invalid authority characters."
            );
            return;
        }

        var proxyOptions = proxyOptionsMonitor.CurrentValue;
        var isTrustedProxy = HostForwardedHeadersExtensions.IsTrustedProxy(
            context.Connection.RemoteIpAddress,
            proxyOptions
        );

        var violation = EvaluateProxyHeaders(context.Request.Headers, proxyOptions, isTrustedProxy);
        if (violation is not null)
        {
            await RejectWithProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                violation.Value.Title,
                violation.Value.Detail
            );
            return;
        }

        if (IsCrossOriginRealtimeRequest(context))
        {
            await RejectWithProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Cross-Origin Realtime Request Forbidden",
                "Cross-origin WebSocket and SignalR hub connections are not permitted."
            );
            return;
        }

        await _next(context);
    }

    private static bool IsCrossOriginRealtimeRequest(HttpContext context)
    {
        var isRealtimeTarget =
            context.WebSockets.IsWebSocketRequest
            || context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments(
                "/_blazor",
                StringComparison.OrdinalIgnoreCase
            );
        if (
            !isRealtimeTarget
            || !context.Request.Headers.TryGetValue("Origin", out var originValues)
        )
        {
            return false;
        }

        var rawOrigin = originValues.ToString().Trim();
        return !string.IsNullOrEmpty(rawOrigin)
            && (
                !Uri.TryCreate(rawOrigin, UriKind.Absolute, out var originUri)
                || !string.Equals(
                    originUri.Host,
                    context.Request.Host.Host,
                    StringComparison.OrdinalIgnoreCase
                )
                || (
                    context.Request.Host.Port.HasValue
                    && !originUri.IsDefaultPort
                    && originUri.Port != context.Request.Host.Port.Value
                )
            );
    }

    private static (string Title, string Detail)? EvaluateProxyHeaders(
        IHeaderDictionary headers,
        ProxyTrustOptions options,
        bool isTrustedProxy
    )
    {
        if (!isTrustedProxy || !options.AllowTrustedForwardedHost)
        {
            foreach (var headerName in _hostOverrideHeaders)
            {
                if (headers.ContainsKey(headerName))
                {
                    return (
                        "Invalid Proxy Header",
                        $"Untrusted host or URL override header '{headerName}' is not permitted."
                    );
                }
            }
        }

        if (isTrustedProxy)
        {
            _ = headers.Remove("X-Real-IP");
            _ = headers.Remove("X-Client-IP");
            _ = headers.Remove("Forwarded");
            return null;
        }

        foreach (var headerName in _forwardedClientHeaders)
        {
            if (!headers.ContainsKey(headerName))
            {
                continue;
            }

            if (options.RejectUntrustedForwardedHeaders)
            {
                return (
                    "Untrusted Forwarded Header",
                    $"Forwarded proxy header '{headerName}' was rejected from an untrusted peer."
                );
            }

            _ = headers.Remove(headerName);
        }

        return null;
    }

    private static bool IsValidHostHeader(IHeaderDictionary headers)
    {
        var hostValues = headers.Host;
        if (hostValues.Count != 1)
        {
            return false;
        }

        var raw = hostValues[0];
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 255)
        {
            return false;
        }

        foreach (var ch in raw)
        {
            if (
                char.IsWhiteSpace(ch)
                || char.IsControl(ch)
                || ch is '@' or '/' or '\\' or '?' or '#' or ','
            )
            {
                return false;
            }
        }

        return IsValidHostAndOptionalPort(raw);
    }

    private static bool IsValidHostAndOptionalPort(string raw)
    {
        string hostPart;
        string? portPart = null;

        if (raw.StartsWith('['))
        {
            var closeIdx = raw.IndexOf(']', StringComparison.Ordinal);
            if (closeIdx <= 1)
            {
                return false;
            }

            hostPart = raw[1..closeIdx];
            if (closeIdx < raw.Length - 1)
            {
                if (raw[closeIdx + 1] != ':')
                {
                    return false;
                }

                portPart = raw[(closeIdx + 2)..];
            }

            return IPAddress.TryParse(hostPart, out _) && IsValidPortOrNull(portPart);
        }

        var colonIdx = raw.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx >= 0)
        {
            hostPart = raw[..colonIdx];
            portPart = raw[(colonIdx + 1)..];
            if (portPart.Contains(':', StringComparison.Ordinal))
            {
                return false;
            }
        }
        else
        {
            hostPart = raw;
        }

        return IsValidDnsOrIpv4Host(hostPart) && IsValidPortOrNull(portPart);
    }

    private static bool IsValidDnsOrIpv4Host(string host)
    {
        if (
            string.IsNullOrEmpty(host)
            || host.StartsWith('-')
            || host.EndsWith('-')
            || host.StartsWith('.')
            || host.EndsWith('.')
            || host.Contains("..", StringComparison.Ordinal)
        )
        {
            return false;
        }

        foreach (var ch in host)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '.')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPortOrNull(string? portPart)
    {
        return portPart is null
            || (
                portPart.Length is >= 1 and <= 5
                && int.TryParse(
                    portPart,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var port
                )
                && port is >= 1 and <= 65535
            );
    }

    private static ValueTask RejectWithProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail
    )
    {
        var problemService = context.RequestServices.GetService<IProblemDetailsService>();
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };
        return ProblemDetailsResponseWriter.WriteAsync(
            context,
            problemService,
            problem,
            cancellationToken: context.RequestAborted
        );
    }
}
