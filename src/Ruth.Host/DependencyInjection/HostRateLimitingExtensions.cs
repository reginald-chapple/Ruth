using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

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
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(CreatePerIpPartition),
                PartitionedRateLimiter.Create<HttpContext, string>(CreatePerTenantPartition)
            );
            _ = options.AddPolicy(RateLimitPolicyNames.AuthStrict, CreateAuthStrictPartition);
            _ = options.AddPolicy(RateLimitPolicyNames.KioskStrict, CreateKioskStrictPartition);
            options.OnRejected = RateLimitRejectionWriter.WriteRejectedAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePerIpPartition(HttpContext httpContext)
    {
        if (IsExemptRequest(httpContext, out var exemptKey))
        {
            return RateLimitPartition.GetNoLimiter(exemptKey);
        }

        var options = GetOptions(httpContext);
        var permitLimit = Math.Clamp(options.PermitLimit, 1, 100_000);
        var windowSeconds = Math.Clamp(options.WindowSeconds, 1, 3600);
        var remoteIp = ResolveClientIpKey(httpContext);
        var partitionKey = string.Create(
            CultureInfo.InvariantCulture,
            $"ip|{remoteIp}|{permitLimit}|{windowSeconds}"
        );
        return CreateFixedWindow(partitionKey, permitLimit, windowSeconds);
    }

    private static RateLimitPartition<string> CreatePerTenantPartition(HttpContext httpContext)
    {
        if (IsExemptRequest(httpContext, out var exemptKey))
        {
            return RateLimitPartition.GetNoLimiter(exemptKey);
        }

        var options = GetOptions(httpContext);
        var permitLimit = Math.Clamp(options.TenantPermitLimit, 1, 500_000);
        var windowSeconds = Math.Clamp(options.TenantWindowSeconds, 1, 3600);
        var tenantKey = ResolveTenantOrHostKey(httpContext);
        var partitionKey = string.Create(
            CultureInfo.InvariantCulture,
            $"tenant|{tenantKey}|{permitLimit}|{windowSeconds}"
        );
        return CreateFixedWindow(partitionKey, permitLimit, windowSeconds);
    }

    private static RateLimitPartition<string> CreateAuthStrictPartition(HttpContext httpContext)
    {
        var options = GetOptions(httpContext);
        var permitLimit = Math.Clamp(options.AuthPermitLimit, 1, 10_000);
        var windowSeconds = Math.Clamp(options.AuthWindowSeconds, 1, 3600);
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"auth|{ResolveTenantOrHostKey(httpContext)}|{ResolveClientIpKey(httpContext)}|{permitLimit}|{windowSeconds}"
        );
        return CreateFixedWindow(key, permitLimit, windowSeconds);
    }

    private static RateLimitPartition<string> CreateKioskStrictPartition(HttpContext httpContext)
    {
        var options = GetOptions(httpContext);
        var permitLimit = Math.Clamp(options.KioskPermitLimit, 1, 10_000);
        var windowSeconds = Math.Clamp(options.KioskWindowSeconds, 1, 3600);
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"kiosk|{ResolveTenantOrHostKey(httpContext)}|{ResolveClientIpKey(httpContext)}|{permitLimit}|{windowSeconds}"
        );
        return CreateFixedWindow(key, permitLimit, windowSeconds);
    }

    private static bool IsExemptRequest(HttpContext httpContext, out string exemptKey)
    {
        if (httpContext.Features.Get<IStatusCodeReExecuteFeature>() is not null)
        {
            exemptKey = "reexecute";
            return true;
        }

        if (
            httpContext.Request.Path.StartsWithSegments(
                "/health",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            exemptKey = "health";
            return true;
        }

        exemptKey = string.Empty;
        return false;
    }

    private static RateLimitPartition<string> CreateFixedWindow(
        string partitionKey,
        int permitLimit,
        int windowSeconds
    )
    {
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

    private static RateLimitingOptions GetOptions(HttpContext httpContext)
    {
        return httpContext
            .RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>()
            .CurrentValue;
    }

    private static string ResolveClientIpKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "loopback";
    }

    private static string ResolveTenantOrHostKey(HttpContext httpContext)
    {
        var currentTenant = AmbientTenantContext.CurrentTenantId;
        if (!currentTenant.IsEmpty)
        {
            return currentTenant.Value.ToString("N");
        }

        var host = httpContext.Request.Host.Host;
        return string.IsNullOrWhiteSpace(host) ? "default" : host.Trim().ToLowerInvariant();
    }
}
