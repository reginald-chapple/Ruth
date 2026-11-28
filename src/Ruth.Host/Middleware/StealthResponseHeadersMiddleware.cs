using Microsoft.Extensions.Primitives;

namespace Ruth.Host.Middleware;

/// <summary>
/// Strips server and framework fingerprinting headers, applies baseline security response headers,
/// and upgrades antiforgery cookies to RFC 6265bis <c lang="http">__Host-csrf</c> over HTTPS.
/// </summary>
internal sealed class StealthResponseHeadersMiddleware(RequestDelegate next)
{
    public const string PlainCsrfCookieName = "csrf";
    public const string SecureHostCsrfCookieName = "__Host-csrf";

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.IsHttps)
        {
            NormalizeHttpsInboundCsrfCookies(context.Request);
        }

        context.Response.OnStarting(
            static state =>
            {
                var httpContext = (HttpContext)state;
                var headers = httpContext.Response.Headers;
                _ = headers.Remove("Server");
                _ = headers.Remove("X-Powered-By");
                _ = headers.Remove("X-AspNet-Version");
                _ = headers.Remove("X-AspNetMvc-Version");
                _ = headers.Remove("X-SourceFiles");
                _ = headers.TryAdd("X-Content-Type-Options", "nosniff");
                _ = headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
                _ = headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

                if (httpContext.Request.IsHttps)
                {
                    UpgradeHttpsOutboundCsrfCookies(headers);
                }

                return Task.CompletedTask;
            },
            context
        );

        return _next(context);
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
                    // Reject unprefixed csrf cookies over HTTPS to block subdomain cookie-tossing.
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
