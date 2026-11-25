using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Onboarding;

internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/onboarding").WithTags("Onboarding").AllowAnonymous();

        _ = group
            .MapGet("/status", GetStatusAsync)
            .WithName("GetOnboardingStatus")
            .WithSummary("Get first-run onboarding status")
            .WithDescription(
                "Returns whether initial system onboarding is required and whether self-registration is locked."
            )
            .Produces<OnboardingStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        _ = group
            .MapPost("/", CompleteAsync)
            .WithName("CompleteOnboarding")
            .WithSummary("Complete initial organization and administrator onboarding")
            .WithDescription(
                "Creates the initial organization profile and administrator account, and permanently locks anonymous self-registration."
            )
            .Produces<CompleteOnboardingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<
        Results<Ok<OnboardingStatusResponse>, ProblemHttpResult>
    > GetStatusAsync(
        IQueryDispatcher queryDispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await queryDispatcher.QueryAsync<
            GetOnboardingStatusQuery,
            OnboardingStatusResponse
        >(new GetOnboardingStatusQuery(), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CompleteOnboardingResponse>, ProblemHttpResult>
    > CompleteAsync(
        CompleteOnboardingRequest request,
        IOnboardingStateReader stateReader,
        ICommandDispatcher commandDispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var currentStatus = await stateReader.GetStatusAsync(cancellationToken);
        if (!currentStatus.IsOnboardingRequired || currentStatus.IsSelfRegistrationLocked)
        {
            return OnboardingProblemResultMapper.ToProblemResult(
                Result.Failure(IdentityErrors.OnboardingAlreadyCompleted),
                httpContext
            );
        }

        var command = new CompleteOnboardingCommand(
            request.OrganizationName,
            request.TimeZoneId,
            request.PrimaryBranchName,
            request.PrimaryBranchShortCode,
            request.AdminUserName,
            request.AdminEmail,
            request.AdminDisplayName,
            request.AdminPassword
        );

        var result = await commandDispatcher.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created(
                new Uri("/api/onboarding/status", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
