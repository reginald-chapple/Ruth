using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Endpoints;

internal static class TenancySignupEndpoints
{
    public static IEndpointRouteBuilder MapTenancySignupEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/v1/platform/signup")
            .WithTags("Platform Signup")
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/", SignupAsync)
            .WithName("SignupTaxExemptTenant")
            .WithSummary("Provision a new Sandbox workspace for a tax-exempt organization")
            .WithDescription(
                "Validates IRS EIN and eligible tax-exempt subsection, creates an isolated Sandbox tenant and subdomain, and seeds initial workspace records."
            );

        return endpoints;
    }

    private static async Task<
        Results<Created<SignupTaxExemptTenantResponse>, NotFound, ProblemHttpResult>
    > SignupAsync(
        SignupTaxExemptTenantCommand command,
        ITenantContext tenantContext,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (tenantContext.HasTenant || tenantContext.IsPlatformOperatorHost)
        {
            return TypedResults.NotFound();
        }

        var result = await dispatcher.SendAsync<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"https://{result.Value.SandboxHostname}", UriKind.Absolute),
                result.Value
            )
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }
}
