using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ruth.Host.Middleware;

namespace Ruth.Host.DependencyInjection;

internal static class RateLimitRejectionWriter
{
    public static async ValueTask WriteRejectedAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken
    )
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var retrySeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            httpContext.Response.Headers.RetryAfter = retrySeconds.ToString(
                CultureInfo.InvariantCulture
            );
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "loopback";
        var logger = httpContext
            .RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(HostRateLimitingExtensions).FullName!);
        HostLog.RateLimitExceeded(
            logger,
            httpContext.Request.Method,
            httpContext.Request.Path,
            remoteIp
        );

        var problemDetailsService =
            httpContext.RequestServices.GetService<IProblemDetailsService>();
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please retry later.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Instance = httpContext.Request.Path,
        };

        await ProblemDetailsResponseWriter.WriteAsync(
            httpContext,
            problemDetailsService,
            problemDetails,
            cancellationToken: cancellationToken
        );
    }
}
