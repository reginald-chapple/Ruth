using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        _ = group
            .MapPost("/login", LoginAsync)
            .WithName("AuthLogin")
            .WithSummary("Authenticate with username/email and password")
            .WithDescription("Issues a signed JWT access token and rotating refresh token.")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/cookie-login", CookieLoginAsync)
            .WithName("AuthCookieLogin")
            .WithSummary("Sign in with an HTTP-only session cookie for Blazor Server")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/cookie-logout", CookieLogoutAsync)
            .WithName("AuthCookieLogout")
            .WithSummary("Sign out of the Blazor Server authentication cookie")
            .Produces(StatusCodes.Status204NoContent)
            .AllowAnonymous();

        _ = group
            .MapPost("/refresh", RefreshAsync)
            .WithName("AuthRefreshToken")
            .WithSummary("Rotate refresh token and issue a fresh JWT access token")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/logout", LogoutAsync)
            .WithName("AuthLogout")
            .WithSummary("Revoke an active refresh token")
            .Produces(StatusCodes.Status204NoContent)
            .AllowAnonymous();

        _ = group
            .MapGet("/me", GetCurrentUserAsync)
            .WithName("AuthGetCurrentUser")
            .WithSummary("Get the currently authenticated user profile and roles")
            .Produces<AuthenticatedUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var command = new LoginCommand(
            request.UserNameOrEmail,
            request.Password,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString()
        );
        var result = await dispatcher.SendAsync<LoginCommand, AuthTokensResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> CookieLoginAsync(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var command = new LoginCommand(
            request.UserNameOrEmail,
            request.Password,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString()
        );
        var result = await dispatcher.SendAsync<LoginCommand, AuthTokensResponse>(
            command,
            cancellationToken
        );
        if (result.IsFailure)
        {
            return OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
        }

        var principal = ClaimsPrincipalExtensions.CreateCookiePrincipal(
            result.Value,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = request.RememberMe }
        );
        return TypedResults.Ok(result.Value);
    }

    private static async Task<NoContent> CookieLogoutAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var command = new RefreshAccessTokenCommand(
            request.RefreshToken,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString()
        );
        var result = await dispatcher.SendAsync<RefreshAccessTokenCommand, AuthTokensResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<NoContent> LogoutAsync(
        RefreshTokenRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        _ = await dispatcher.SendAsync<LogoutCommand, bool>(
            new LogoutCommand(request.RefreshToken),
            cancellationToken
        );
        return TypedResults.NoContent();
    }

    private static async Task<
        Results<Ok<AuthenticatedUserResponse>, ProblemHttpResult>
    > GetCurrentUserAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.GetUserAccountId();
        if (!userId.HasValue)
        {
            return OnboardingProblemResultMapper.ToProblemResult(
                SharedKernel.Results.Result.Failure(IdentityErrors.InvalidCredentials),
                httpContext
            );
        }

        var result = await dispatcher.QueryAsync<GetCurrentUserQuery, AuthenticatedUserResponse>(
            new GetCurrentUserQuery(userId.Value),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
