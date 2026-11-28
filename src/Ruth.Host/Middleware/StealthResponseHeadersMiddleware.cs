using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace Ruth.Host.Middleware;

/// <summary>
/// Strips server and framework fingerprinting headers, applies strict defense-in-depth HTTP security headers
/// (including per-request cryptographic CSP nonces), and upgrades antiforgery cookies to RFC 6265bis <c lang="http">__Host-csrf</c> over HTTPS.
/// </summary>
internal sealed class StealthResponseHeadersMiddleware(RequestDelegate next)
{
    public const string PlainCsrfCookieName = "csrf";
    public const string SecureHostCsrfCookieName = "__Host-csrf";
    public const string CspNonceItemKey = "Stealth.CspNonce";

    private const string PermissionsPolicyHeaderValue =
        "accelerometer=(), autoplay=(), camera=(), display-capture=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), usb=()";

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cspNonce = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        context.Items[CspNonceItemKey] = cspNonce;

        if (context.Request.IsHttps)
        {
            NormalizeHttpsInboundCsrfCookies(context.Request);
        }

        context.Response.OnStarting(
            static state =>
            {
                var (httpContext, nonce) = ((HttpContext, string))state;
                ApplyStealthAndSecurityHeaders(httpContext, nonce);
                return Task.CompletedTask;
            },
            (context, cspNonce)
        );

        return _next(context);
    }

    private static void ApplyStealthAndSecurityHeaders(HttpContext httpContext, string nonce)
    {
        var headers = httpContext.Response.Headers;
        _ = headers.Remove("Server");
        _ = headers.Remove("X-Powered-By");
        _ = headers.Remove("X-AspNet-Version");
        _ = headers.Remove("X-AspNetMvc-Version");
        _ = headers.Remove("X-AspNetCore-Version");
        _ = headers.Remove("X-SourceFiles");
        _ = headers.Remove("X-Generator");
        _ = headers.Remove("X-Runtime");

        _ = headers.TryAdd("X-Content-Type-Options", "nosniff");
        _ = headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
        _ = headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        _ = headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
        _ = headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
        _ = headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
        _ = headers.TryAdd("Permissions-Policy", PermissionsPolicyHeaderValue);
        headers.ContentSecurityPolicy = BuildContentSecurityPolicy(
            nonce,
            httpContext.Request.IsHttps
        );

        if (httpContext.Request.IsHttps)
        {
            UpgradeHttpsOutboundCsrfCookies(headers);
        }
    }

    private static string BuildContentSecurityPolicy(string nonce, bool isHttps)
    {
        var basePolicy =
            $"default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; form-action 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: https:; font-src 'self' data:; connect-src 'self' ws: wss:";
        return isHttps ? $"{basePolicy}; upgrade-insecure-requests" : basePolicy;
    }

    private static void NormalizeHttpsInboundCsrfCookies(HttpRequest request)
    {
        var rawCookieHeaders = request.Headers.Cookie;
        if (StringValues.IsNullOrEmpty(rawCookieHeaders))
        {
            return;
        }

        List<string> rewrittenPairs = [];
        var modified = false;

        foreach (var headerLine in rawCookieHeaders)
        {
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                continue;
            }

            foreach (var segment in headerLine.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = segment.Trim();
                if (trimmed.StartsWith(PlainCsrfCookieName + "=", StringComparison.Ordinal))
                {
                    modified = true;
                    continue;
                }

                if (trimmed.StartsWith(SecureHostCsrfCookieName + "=", StringComparison.Ordinal))
                {
                    var valuePart = trimmed[(SecureHostCsrfCookieName.Length + 1)..];
                    rewrittenPairs.Add($"{PlainCsrfCookieName}={valuePart}");
                    modified = true;
                    continue;
                }

                rewrittenPairs.Add(trimmed);
            }
        }

        if (modified)
        {
            request.Headers.Cookie =
                rewrittenPairs.Count == 0
                    ? StringValues.Empty
                    : new StringValues(string.Join("; ", rewrittenPairs));
        }
    }

    private static void UpgradeHttpsOutboundCsrfCookies(IHeaderDictionary headers)
    {
        var setCookies = headers.SetCookie;
        if (StringValues.IsNullOrEmpty(setCookies))
        {
            return;
        }

        var updated = new string?[setCookies.Count];
        var changed = false;

        for (var i = 0; i < setCookies.Count; i++)
        {
            var entry = setCookies[i];
            if (
                entry is not null
                && entry.StartsWith(PlainCsrfCookieName + "=", StringComparison.Ordinal)
            )
            {
                var rest = entry[(PlainCsrfCookieName.Length + 1)..];
                var upgraded = $"{SecureHostCsrfCookieName}={rest}";
                if (!upgraded.Contains("path=/", StringComparison.OrdinalIgnoreCase))
                {
                    upgraded += "; path=/";
                }

                if (!upgraded.Contains("; secure", StringComparison.OrdinalIgnoreCase))
                {
                    upgraded += "; secure";
                }

                updated[i] = upgraded;
                changed = true;
            }
            else
            {
                updated[i] = entry;
            }
        }

        if (changed)
        {
            headers.SetCookie = new StringValues(updated);
        }
    }
}
