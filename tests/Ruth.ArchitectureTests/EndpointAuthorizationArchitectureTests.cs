using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Ruth.ArchitectureTests;

/// <summary>
/// Enforces that 100% of registered HTTP and SignalR endpoints explicitly declare either
/// <see cref="IAuthorizeData" /> or an audited <see cref="IAllowAnonymous" /> policy.
/// </summary>
public sealed class EndpointAuthorizationArchitectureTests
{
    private static readonly string[] _approvedPublicRoutePrefixes =
    [
        "/health",
        "/api/onboarding",
        "/api/v1/platform/signup",
        "/api/v1/platform/operators/authenticate",
        "/api/v1/platform/operators/logout",
        "/api/v1/tenancy/webhooks/stripe-connect",
        "/api/auth/login",
        "/api/auth/cookie-login",
        "/api/auth/cookie-logout",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/api/auth/password-reset/",
        "/api/auth/confirm-account",
        "/api/auth/oidc/providers",
        "/api/auth/oidc/",
        "/api/crm/webhooks/background-checks/",
        "/api/groups/scheduling/rsvp/",
        "/api/communication/webhooks/events",
        "/api/communication/preferences",
        "/api/communication/sms/webhook",
        "/api/finance/gateways/",
        "/api/finance/campaigns/public",
        "/api/events/calendars/",
        "/api/events/interactive-sessions/live",
        "/api/events/registrations/public/",
        "/hubs/events/interactive",
        "/api/commerce/storefront/",
        "/api/commerce/bookings/public/",
        "/api/engagement/public/",
        "/api/workflows/public/",
        "/api/workflows/webhooks/",
        "/api/cms/public",
        "/api/cms/themes/",
        "/api/cms/datasets/",
        "/api/cms/interactions",
        "/api/v1/mobile/apps/",
        "/api/v1/signage/",
        "/go/",
        "/api/lms/catalog",
        "/api/search/public",
    ];

    private static readonly string[] _approvedPublicRouteExactOrSuffixes =
    [
        "/feed.rss",
        "/feed.json",
    ];

    [Fact]
    public async Task Every_Registered_Endpoint_Must_Explicitly_Declare_Authorization_Or_Approved_AllowAnonymous()
    {
        await using var app =
            EndpointAuthorizationTestAppFactory.BuildUnstartedAppWithAllEndpoints();

        var routeEndpoints = ((IEndpointRouteBuilder)app)
            .DataSources.SelectMany(static ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        Assert.True(routeEndpoints.Count >= 100);

        List<string> undeclaredEndpoints = [];
        List<string> unapprovedAnonymousEndpoints = [];

        foreach (var endpoint in routeEndpoints)
        {
            var rawPattern = "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');
            var hasAuthorize = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;
            var hasAllowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (!hasAuthorize && !hasAllowAnonymous)
            {
                undeclaredEndpoints.Add($"{rawPattern} ({endpoint.DisplayName})");
                continue;
            }

            if (hasAllowAnonymous && !IsApprovedPublicRoute(rawPattern))
            {
                unapprovedAnonymousEndpoints.Add($"{rawPattern} ({endpoint.DisplayName})");
            }
        }

        Assert.True(
            undeclaredEndpoints.Count == 0,
            $"Endpoints missing explicit RequireAuthorization or AllowAnonymous metadata:\n{string.Join('\n', undeclaredEndpoints)}"
        );
        Assert.True(
            unapprovedAnonymousEndpoints.Count == 0,
            $"Unreviewed AllowAnonymous endpoints outside approved public surface:\n{string.Join('\n', unapprovedAnonymousEndpoints)}"
        );
    }

    private static bool IsApprovedPublicRoute(string normalizedPattern)
    {
        foreach (var prefix in _approvedPublicRoutePrefixes)
        {
            if (normalizedPattern.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var suffix in _approvedPublicRouteExactOrSuffixes)
        {
            if (normalizedPattern.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
