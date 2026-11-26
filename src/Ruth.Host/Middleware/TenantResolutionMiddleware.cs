using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Resolves incoming HTTP Host headers to isolated tenant workspaces or dedicated platform hosts,
/// returning an empty 404 Not Found on unknown, unverified, or cross-plane requests.
/// </summary>
internal sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string StealthPlainNotFoundItemKey = "Stealth.PlainNotFound";

    private static readonly string[] _sharedFrameworkPrefixes =
    [
        "/_framework",
        "/_content",
        "/_blazor",
        "/openapi",
    ];

    private static readonly string[] _staticExtensions =
    [
        ".css",
        ".js",
        ".mjs",
        ".map",
        ".svg",
        ".ico",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".webmanifest",
    ];

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(
        HttpContext context,
        ITenantHostResolver hostResolver,
        TenantWorkspaceBrandingState brandingState
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hostResolver);
        ArgumentNullException.ThrowIfNull(brandingState);

        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var resolved = await hostResolver.ResolveHostAsync(
            context.Request.Host.Host,
            context.RequestAborted
        );
        brandingState.Initialize(resolved);

        switch (resolved.Kind)
        {
            case ResolvedHostKind.PlatformOperator:
                await InvokeOperatorHostAsync(context);
                return;
            case ResolvedHostKind.PlatformSignup:
                await InvokeSignupHostAsync(context);
                return;
            case ResolvedHostKind.TenantWorkspace:
                await InvokeTenantHostAsync(context, resolved);
                return;
            case ResolvedHostKind.Unknown:
            default:
                WriteStealthNotFound(context);
                return;
        }
    }

    private async Task InvokeOperatorHostAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (string.Equals(path.Value, "/", StringComparison.Ordinal) && IsGetOrHead(context))
        {
            context.Response.Redirect("/ops", permanent: false);
            return;
        }

        if (
            !path.StartsWithSegments("/ops", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments(
                "/api/v1/platform/operators",
                StringComparison.OrdinalIgnoreCase
            )
            && !IsSharedShellAsset(path)
        )
        {
            WriteStealthNotFound(context);
            return;
        }

        using (AmbientTenantContext.BeginPlatformOperatorScope())
        {
            await _next(context);
        }
    }

    private async Task InvokeSignupHostAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (string.Equals(path.Value, "/", StringComparison.Ordinal) && IsGetOrHead(context))
        {
            context.Response.Redirect("/signup", permanent: false);
            return;
        }

        if (
            !path.StartsWithSegments("/signup", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments(
                "/api/v1/platform/signup",
                StringComparison.OrdinalIgnoreCase
            )
            && !IsSharedShellAsset(path)
        )
        {
            WriteStealthNotFound(context);
            return;
        }

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            await _next(context);
        }
    }

    private async Task InvokeTenantHostAsync(HttpContext context, ResolvedHostResponse resolved)
    {
        var path = context.Request.Path;
        if (
            path.StartsWithSegments("/signup", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/ops", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/v1/platform", StringComparison.OrdinalIgnoreCase)
        )
        {
            WriteStealthNotFound(context);
            return;
        }

        using (
            AmbientTenantContext.BeginScope(
                resolved.TenantId,
                resolved.TenantSlug,
                resolved.LifecycleStage
            )
        )
        {
            await _next(context);
        }
    }

    private static bool IsSharedShellAsset(PathString path)
    {
        foreach (var prefix in _sharedFrameworkPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var raw = path.Value;
        if (
            string.IsNullOrEmpty(raw)
            || path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        foreach (var ext in _staticExtensions)
        {
            if (raw.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGetOrHead(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method);
    }

    private static void WriteStealthNotFound(HttpContext context)
    {
        context.Items[StealthPlainNotFoundItemKey] = true;
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentLength = 0;
    }
}
