using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal static class OidcAuthEndpoints
{
    public static IEndpointRouteBuilder MapOidcAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/auth/oidc").WithTags("External OIDC");

        _ = group
            .MapGet("/providers", GetProvidersAsync)
            .WithName("GetExternalAuthProviders")
            .WithSummary("List configured external OIDC providers")
            .Produces<IReadOnlyList<ExternalAuthProviderResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        _ = group
            .MapPut("/providers", UpsertProviderAsync)
            .WithName("UpsertExternalAuthProvider")
            .WithSummary("Create or update an external OIDC provider configuration")
            .Produces<ExternalAuthProviderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);

        _ = group
            .MapPost("/{providerKey}/start", StartLoginAsync)
            .WithName("StartOidcLogin")
            .WithSummary("Start a PKCE-protected external OIDC sign-in session")
            .Produces<StartOidcLoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/{providerKey}/callback", CompleteLoginAsync)
            .WithName("CompleteOidcLogin")
            .WithSummary("Complete an external OIDC callback and exchange for Ruth tokens")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ExternalAuthProviderResponse>>, ProblemHttpResult>
    > GetProvidersAsync(
        bool? enabledOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetExternalAuthProvidersQuery,
            IReadOnlyList<ExternalAuthProviderResponse>
        >(new GetExternalAuthProvidersQuery(enabledOnly ?? true), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<ExternalAuthProviderResponse>, ProblemHttpResult>
    > UpsertProviderAsync(
        UpsertExternalAuthProviderRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpsertExternalAuthProviderCommand,
            ExternalAuthProviderResponse
        >(
            new UpsertExternalAuthProviderCommand(
                request.ProviderKey,
                request.DisplayName,
                request.Authority,
                request.ClientId,
                request.ClientSecret,
                request.Scopes,
                request.IsEnabled,
                request.AutoLinkByEmail,
                request.CallbackPath
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<StartOidcLoginResponse>, ProblemHttpResult>
    > StartLoginAsync(
        string providerKey,
        string? returnUrl,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<StartOidcLoginCommand, StartOidcLoginResponse>(
            new StartOidcLoginCommand(providerKey, returnUrl),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AuthTokensResponse>, ProblemHttpResult>
    > CompleteLoginAsync(
        string providerKey,
        CompleteOidcLoginRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CompleteOidcLoginCommand, AuthTokensResponse>(
            new CompleteOidcLoginCommand(
                providerKey,
                request.State,
                request.Code,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                request.RedirectUri
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
