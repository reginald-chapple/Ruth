using Microsoft.AspNetCore.Mvc;
using Ruth.Modules.Identity.Contracts.Onboarding;

namespace Ruth.Host.Middleware;

internal sealed class OnboardingGateMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService
)
{
    private static readonly string[] _exemptPrefixes =
    [
        "/api/onboarding",
        "/health",
        "/_framework",
        "/_content",
        "/_blazor",
        "/openapi",
    ];

    private static readonly string[] _staticAssetExtensions =
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
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService
        ?? throw new ArgumentNullException(nameof(problemDetailsService));

    public async Task InvokeAsync(HttpContext context, IOnboardingStateReader onboardingStateReader)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onboardingStateReader);

        if (IsAlwaysExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var status = await onboardingStateReader.GetStatusAsync(context.RequestAborted);
        var isOnboardingRoute = context.Request.Path.StartsWithSegments(
            "/onboarding",
            StringComparison.OrdinalIgnoreCase
        );

        if (status.IsOnboardingRequired)
        {
            await HandleUnonboardedRequestAsync(context, isOnboardingRoute);
            return;
        }

        if (isOnboardingRoute)
        {
            await HandleLockedOnboardingRouteAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleUnonboardedRequestAsync(HttpContext context, bool isOnboardingRoute)
    {
        if (isOnboardingRoute)
        {
            await _next(context);
            return;
        }

        if (IsBrowserGetOrHead(context.Request))
        {
            context.Response.Redirect("/onboarding", permanent: false);
            return;
        }

        await WriteProblemAsync(
            context,
            StatusCodes.Status503ServiceUnavailable,
            "Onboarding Required",
            "Initial system onboarding has not been completed. Complete setup at /onboarding.",
            "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            "Onboarding.Required",
            onboardingUrl: "/onboarding"
        );
    }

    private async Task HandleLockedOnboardingRouteAsync(HttpContext context)
    {
        if (IsGetOrHead(context.Request.Method))
        {
            context.Response.Redirect("/", permanent: false);
            return;
        }

        await WriteProblemAsync(
            context,
            StatusCodes.Status409Conflict,
            "Onboarding Locked",
            "System onboarding is already complete and self-registration is permanently locked.",
            "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            "Onboarding.Locked"
        );
    }

    private async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string typeReference,
        string errorCode,
        string? onboardingUrl = null
    )
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = typeReference,
            Instance = context.Request.Path,
        };
        problem.Extensions["errorCode"] = errorCode;

        if (onboardingUrl is not null)
        {
            problem.Extensions["onboardingUrl"] = onboardingUrl;
        }

        await ProblemDetailsResponseWriter.WriteAsync(
            context,
            _problemDetailsService,
            problem,
            cancellationToken: context.RequestAborted
        );
    }

    private static bool IsAlwaysExemptPath(PathString path)
    {
        foreach (var prefix in _exemptPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return IsStaticAssetPath(path);
    }

    private static bool IsStaticAssetPath(PathString path)
    {
        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rawPath = path.Value;
        if (string.IsNullOrEmpty(rawPath))
        {
            return false;
        }

        foreach (var extension in _staticAssetExtensions)
        {
            if (rawPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBrowserGetOrHead(HttpRequest request)
    {
        return IsGetOrHead(request.Method)
            && !request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGetOrHead(string method)
    {
        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
    }
}
