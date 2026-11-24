using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.Middleware;

namespace Ruth.Host.DependencyInjection;

internal static class HostRateLimitingExtensions
{
    public static IServiceCollection AddRuthRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        _ = services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                CreatePartition
            );
            options.OnRejected = HandleRejectedAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(HttpContext httpContext)
    {
        if (httpContext.Features.Get<IStatusCodeReExecuteFeature>() is not null)
        {
            return RateLimitPartition.GetNoLimiter("reexecute");
        }

        if (
            httpContext.Request.Path.StartsWithSegments(
                "/health",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var options = httpContext
            .RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>()
            .CurrentValue;
        var permitLimit = Math.Clamp(options.PermitLimit, 1, 100_000);
        var windowSeconds = Math.Clamp(options.WindowSeconds, 1, 3600);

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "loopback";
        var partitionKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{remoteIp}|{permitLimit}|{windowSeconds}"
        );
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true,
            }
        );
    }

    private static async ValueTask HandleRejectedAsync(
        Microsoft.AspNetCore.RateLimiting.OnRejectedContext context,
        CancellationToken cancellationToken
    )
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var logger = httpContext
            .RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(HostRateLimitingExtensions).FullName!);
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "loopback";

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
