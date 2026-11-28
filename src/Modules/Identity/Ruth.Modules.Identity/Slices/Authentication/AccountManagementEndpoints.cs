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

internal static class AccountManagementEndpoints
{
    public static IEndpointRouteBuilder MapAccountManagementEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/auth").WithTags("Account Management");

        _ = group
            .MapPost("/change-password", ChangePasswordAsync)
            .WithName("AuthChangePassword")
            .WithSummary("Change the current user's password")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        MapRecoveryEndpoints(group);
        MapAdminUserEndpoints(group);
        return endpoints;
    }

    private static void MapRecoveryEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/password-reset/request", RequestPasswordResetAsync)
            .WithName("AuthRequestPasswordReset")
            .WithSummary("Request a single-use password reset token")
            .Produces(StatusCodes.Status202Accepted)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/password-reset/confirm", ConfirmPasswordResetAsync)
            .WithName("AuthConfirmPasswordReset")
            .WithSummary("Confirm a password reset using a single-use token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();

        _ = group
            .MapPost("/confirm-account", ConfirmAccountAsync)
            .WithName("AuthConfirmAccount")
            .WithSummary("Confirm a user account using a single-use confirmation token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(RateLimitPolicyNames.AuthStrict)
            .AllowAnonymous();
    }

    private static void MapAdminUserEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/login-history", GetLoginHistoryAsync)
            .WithName("AuthGetLoginHistory")
            .WithSummary("Retrieve recent login history entries")
            .Produces<IReadOnlyList<LoginHistoryItemResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        _ = group
            .MapGet("/users", GetUsersAsync)
            .WithName("AuthListUsers")
            .WithSummary("List user accounts (Administrators only)")
            .Produces<IReadOnlyList<UserAccountSummaryResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);

        _ = group
            .MapPost("/users", CreateUserAsync)
            .WithName("AuthCreateUser")
            .WithSummary("Provision a new user account (Administrators only)")
            .Produces<UserAccountSummaryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangePasswordAsync(
        ChangePasswordRequest request,
        ICommandDispatcher dispatcher,
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

        var result = await dispatcher.SendAsync<ChangePasswordCommand, bool>(
            new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Accepted> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        _ = await dispatcher.SendAsync<RequestPasswordResetCommand, IssuedTokenResponse>(
            new RequestPasswordResetCommand(request.EmailOrUserName),
            cancellationToken
        );
        return TypedResults.Accepted(uri: (string?)null);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmPasswordResetAsync(
        ConfirmPasswordResetRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<ConfirmPasswordResetCommand, bool>(
            new ConfirmPasswordResetCommand(request.Token, request.NewPassword),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmAccountAsync(
        ConfirmAccountRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<ConfirmAccountCommand, bool>(
            new ConfirmAccountCommand(request.Token),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<LoginHistoryItemResponse>>, ProblemHttpResult>
    > GetLoginHistoryAsync(
        Guid? userAccountId,
        int? maxCount,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var callerId = httpContext.User.GetUserAccountId();
        var isAdmin = httpContext.User.IsInRole(SecurityRole.AdministratorsRoleName);
        if (!isAdmin && userAccountId.HasValue && userAccountId.Value != callerId)
        {
            return OnboardingProblemResultMapper.ToProblemResult(
                SharedKernel.Results.Result.Failure(IdentityErrors.ForbiddenAccess),
                httpContext
            );
        }

        var effectiveUserId = isAdmin ? userAccountId : callerId;

        var result = await dispatcher.QueryAsync<
            GetLoginHistoryQuery,
            IReadOnlyList<LoginHistoryItemResponse>
        >(new GetLoginHistoryQuery(effectiveUserId, maxCount ?? 100), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<UserAccountSummaryResponse>>, ProblemHttpResult>
    > GetUsersAsync(
        string? search,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetUserAccountsQuery,
            IReadOnlyList<UserAccountSummaryResponse>
        >(new GetUserAccountsQuery(search), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<UserAccountSummaryResponse>, ProblemHttpResult>
    > CreateUserAsync(
        CreateUserAccountRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateUserAccountCommand,
            UserAccountSummaryResponse
        >(
            new CreateUserAccountCommand(
                request.UserName,
                request.Email,
                request.DisplayName,
                request.Password,
                request.IsConfirmed,
                request.RoleIds
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/auth/users/{result.Value.UserAccountId:D}", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
