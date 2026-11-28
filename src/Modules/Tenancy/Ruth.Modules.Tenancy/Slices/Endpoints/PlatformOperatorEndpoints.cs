using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Endpoints;

internal static class PlatformOperatorEndpoints
{
    public const string OperatorAuthorizationPolicy = "PlatformOperatorPolicy";

    public static IEndpointRouteBuilder MapPlatformOperatorEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var authGroup = endpoints
            .MapGroup("/api/v1/platform/operators")
            .WithTags("Platform Operators");

        _ = authGroup
            .MapPost("/authenticate", AuthenticateAsync)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous()
            .WithName("AuthenticatePlatformOperator")
            .WithSummary("Authenticate a Platform Operator on the isolated operator host")
            .WithDescription(
                "Validates Platform Operator credentials on ops.<platform-domain> and issues a host-only operator session cookie."
            );

        _ = authGroup
            .MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .WithName("LogoutPlatformOperator")
            .WithSummary("Sign out of the Platform Operator session")
            .WithDescription("Clears the host-only Platform Operator authentication cookie.");

        _ = authGroup
            .MapGet("/queue", GetQueueAsync)
            .RequireAuthorization(OperatorAuthorizationPolicy)
            .WithName("GetPendingGoLiveQueue")
            .WithSummary("List organization tenants in the Platform Operator review queue")
            .WithDescription(
                "Returns registered tax-exempt tenants filtered by optional lifecycle stage on the isolated operator host."
            );

        _ = authGroup
            .MapPost("/review", ReviewAsync)
            .RequireAuthorization(OperatorAuthorizationPolicy)
            .WithName("ReviewTenantGoLive")
            .WithSummary("Approve Live, return to Sandbox, or suspend a tenant")
            .WithDescription(
                "Records an authenticated Platform Operator's Go-Live review decision and updates custom domain routing."
            );

        return endpoints;
    }

    private static async Task<
        Results<Ok<PlatformOperatorAuthResponse>, NotFound, ProblemHttpResult>
    > AuthenticateAsync(
        AuthenticatePlatformOperatorCommand command,
        ITenantContext tenantContext,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (!tenantContext.IsPlatformOperatorHost)
        {
            return TypedResults.NotFound();
        }

        var result = await dispatcher.SendAsync<
            AuthenticatePlatformOperatorCommand,
            PlatformOperatorAuthResponse
        >(command, cancellationToken);
        if (result.IsFailure)
        {
            return TenancyProblemResultMapper.ToProblem(result, httpContext);
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, result.Value.OperatorId.ToString("D")),
            new(ClaimTypes.Name, result.Value.Email),
            new(ClaimTypes.Email, result.Value.Email),
            new("display_name", result.Value.DisplayName),
            new("platform_operator", "true"),
        ];
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
        );
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<NoContent, NotFound>> LogoutAsync(
        ITenantContext tenantContext,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!tenantContext.IsPlatformOperatorHost)
        {
            return TypedResults.NotFound();
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TenantSummaryResponse>>, NotFound, ProblemHttpResult>
    > GetQueueAsync(
        TenantLifecycleStage? stage,
        ITenantContext tenantContext,
        IQueryDispatcher queryDispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (!tenantContext.IsPlatformOperatorHost)
        {
            return TypedResults.NotFound();
        }

        var result = await queryDispatcher.QueryAsync<
            GetPendingGoLiveQueueQuery,
            IReadOnlyList<TenantSummaryResponse>
        >(new GetPendingGoLiveQueueQuery(stage), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<TenantComplianceStatusResponse>, NotFound, ProblemHttpResult>
    > ReviewAsync(
        ReviewTenantGoLiveRequest request,
        ITenantContext tenantContext,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (!tenantContext.IsPlatformOperatorHost)
        {
            return TypedResults.NotFound();
        }

        var authenticatedOperatorEmail = httpContext.User.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;

        var result = await dispatcher.SendAsync<
            ReviewTenantGoLiveCommand,
            TenantComplianceStatusResponse
        >(
            new ReviewTenantGoLiveCommand(
                request.TargetTenantId,
                request.Decision,
                authenticatedOperatorEmail,
                request.Notes,
                request.ManualIrsOverrideApproved
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }
}
