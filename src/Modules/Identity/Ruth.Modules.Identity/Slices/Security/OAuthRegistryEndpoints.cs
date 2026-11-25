using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Slices.Security;

internal static class OAuthRegistryEndpoints
{
    public static IEndpointRouteBuilder MapOAuthRegistryEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/security")
            .WithTags("OAuth & OIDC Registry")
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);

        _ = group
            .MapGet("/clients", GetClientsAsync)
            .WithName("GetAuthClients")
            .WithSummary("List registered OAuth/OIDC clients")
            .Produces<IReadOnlyList<AuthClientResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/clients", CreateClientAsync)
            .WithName("CreateAuthClient")
            .WithSummary("Register a new OAuth/OIDC client")
            .Produces<AuthClientResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/scopes", GetScopesAsync)
            .WithName("GetAuthScopes")
            .WithSummary("List registered OAuth/OIDC scopes")
            .Produces<IReadOnlyList<AuthScopeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/scopes", CreateScopeAsync)
            .WithName("CreateAuthScope")
            .WithSummary("Create a new OAuth/OIDC scope")
            .Produces<AuthScopeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AuthClientResponse>>, ProblemHttpResult>
    > GetClientsAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAuthClientsQuery,
            IReadOnlyList<AuthClientResponse>
        >(new GetAuthClientsQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AuthClientResponse>, ProblemHttpResult>
    > CreateClientAsync(
        CreateAuthClientRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateAuthClientCommand, AuthClientResponse>(
            new CreateAuthClientCommand(
                request.Name,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                request.PostLogoutRedirectUri,
                request.AllowedScopes,
                request.AllowedClaims,
                request.AllowUserApiAccess,
                request.ScopeApprovalExpirationDays
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/security/clients/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AuthScopeResponse>>, ProblemHttpResult>
    > GetScopesAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAuthScopesQuery,
            IReadOnlyList<AuthScopeResponse>
        >(new GetAuthScopesQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AuthScopeResponse>, ProblemHttpResult>
    > CreateScopeAsync(
        CreateAuthScopeRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateAuthScopeCommand, AuthScopeResponse>(
            new CreateAuthScopeCommand(request.Name, request.PublicName),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/security/scopes/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
