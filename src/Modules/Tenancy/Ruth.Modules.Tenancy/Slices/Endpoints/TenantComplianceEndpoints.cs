using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Slices.Endpoints;

internal static class TenantComplianceEndpoints
{
    public static IEndpointRouteBuilder MapTenantComplianceEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/v1/tenancy")
            .WithTags("Organization Compliance")
            .RequireAuthorization("Ruth.AdministratorsOnly");

        _ = group
            .MapGet("/compliance", GetComplianceAsync)
            .WithName("GetCurrentTenantCompliance")
            .WithSummary("Get current organization compliance and domain status")
            .WithDescription(
                "Returns IRS EO BMF verification status, Stripe Connect payout readiness, lifecycle stage, and registered domains for the active workspace."
            );
        _ = group
            .MapPost("/stripe-connect/onboarding", StartStripeOnboardingAsync)
            .WithName("StartStripeConnectOnboarding")
            .WithSummary("Create or resume a Stripe Connect Express onboarding session")
            .WithDescription(
                "Creates or resumes a Stripe Connect Express account link for the active tax-exempt organization."
            );
        _ = group
            .MapPost("/stripe-connect/mock-complete", CompleteMockStripeAsync)
            .WithName("CompleteMockStripeConnect")
            .WithSummary("Complete simulated Stripe Connect payout readiness in mock mode")
            .WithDescription(
                "Marks charges and payouts enabled on the organization's mock Stripe Connect account when StripeMockMode is active."
            );
        _ = group
            .MapPost("/domains", RegisterCustomDomainAsync)
            .WithName("RegisterTenantCustomDomain")
            .WithSummary("Register a custom organization domain")
            .WithDescription(
                "Registers an organization-owned custom domain and generates a DNS TXT ownership verification challenge."
            );
        _ = group
            .MapPost("/domains/{domainId:guid}/verify", VerifyCustomDomainAsync)
            .WithName("VerifyTenantCustomDomain")
            .WithSummary("Verify DNS TXT ownership for a custom domain")
            .WithDescription(
                "Checks DNS TXT records for the domain's verification challenge and promotes primary routing if the workspace is Live."
            );
        _ = group
            .MapPost("/go-live/submit", SubmitGoLiveAsync)
            .WithName("SubmitTenantForGoLive")
            .WithSummary("Run IRS EO BMF verification and submit workspace for Go-Live approval")
            .WithDescription(
                "Verifies Stripe Connect payout readiness and IRS Exempt Organizations Business Master File status before queuing for Platform Operator approval."
            );

        return endpoints;
    }

    private static async Task<
        Results<Ok<TenantComplianceStatusResponse>, ProblemHttpResult>
    > GetComplianceAsync(
        IQueryDispatcher queryDispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await queryDispatcher.QueryAsync<
            GetCurrentTenantComplianceQuery,
            TenantComplianceStatusResponse
        >(new GetCurrentTenantComplianceQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<StripeConnectOnboardingResponse>, ProblemHttpResult>
    > StartStripeOnboardingAsync(
        StartStripeConnectOnboardingCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            StartStripeConnectOnboardingCommand,
            StripeConnectOnboardingResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<TenantComplianceStatusResponse>, ProblemHttpResult>
    > CompleteMockStripeAsync(
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CompleteMockStripeConnectCommand,
            TenantComplianceStatusResponse
        >(new CompleteMockStripeConnectCommand(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Created<TenantDomainResponse>, ProblemHttpResult>
    > RegisterCustomDomainAsync(
        RegisterCustomDomainCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RegisterCustomDomainCommand, TenantDomainResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/tenancy/domains/{result.Value.Id:D}", result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<TenantDomainResponse>, ProblemHttpResult>
    > VerifyCustomDomainAsync(
        Guid domainId,
        VerifyDomainTokenRequest? request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<VerifyCustomDomainCommand, TenantDomainResponse>(
            new VerifyCustomDomainCommand(domainId, request?.ObservedTxtToken),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<TenantComplianceStatusResponse>, ProblemHttpResult>
    > SubmitGoLiveAsync(
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SubmitTenantForGoLiveCommand,
            TenantComplianceStatusResponse
        >(new SubmitTenantForGoLiveCommand(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }
}
