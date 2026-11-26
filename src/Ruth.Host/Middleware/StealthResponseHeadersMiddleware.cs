namespace Ruth.Host.Middleware;

/// <summary>
/// Strips server and framework fingerprinting headers and applies baseline security response headers
/// on every HTTP response.
/// </summary>
internal sealed class StealthResponseHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(
            static state =>
            {
                var headers = ((HttpContext)state).Response.Headers;
                _ = headers.Remove("Server");
                _ = headers.Remove("X-Powered-By");
                _ = headers.Remove("X-AspNet-Version");
                _ = headers.Remove("X-AspNetMvc-Version");
                _ = headers.TryAdd("X-Content-Type-Options", "nosniff");
                _ = headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
                _ = headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                return Task.CompletedTask;
            },
            context
        );

        return _next(context);
    }
}
