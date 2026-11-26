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

        _ = group.MapGet("/compliance", GetComplianceAsync).WithName("GetCurrentTenantCompliance");
        _ = group
            .MapPost("/stripe-connect/onboarding", StartStripeOnboardingAsync)
            .WithName("StartStripeConnectOnboarding");
        _ = group
            .MapPost("/stripe-connect/mock-complete", CompleteMockStripeAsync)
            .WithName("CompleteMockStripeConnect");
        _ = group
            .MapPost("/domains", RegisterCustomDomainAsync)
            .WithName("RegisterTenantCustomDomain");
        _ = group
            .MapPost("/domains/{domainId:guid}/verify", VerifyCustomDomainAsync)
            .WithName("VerifyTenantCustomDomain");
        _ = group.MapPost("/go-live/submit", SubmitGoLiveAsync).WithName("SubmitTenantForGoLive");

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
        Results<Ok<TenantDomainResponse>, ProblemHttpResult>
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
            ? TypedResults.Ok(result.Value)
            : TenancyProblemResultMapper.ToProblem(result, httpContext);
    }

    private static async Task<
        Results<Ok<TenantDomainResponse>, ProblemHttpResult>
    > VerifyCustomDomainAsync(
        Guid domainId,
        VerifyDomainTokenRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<VerifyCustomDomainCommand, TenantDomainResponse>(
            new VerifyCustomDomainCommand(domainId, request.ObservedTxtToken),
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
