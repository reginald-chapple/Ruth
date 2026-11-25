using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Verification;

internal static class VerificationAndViewedEndpoints
{
    public static IEndpointRouteBuilder MapVerificationAndViewedEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var verification = endpoints
            .MapGroup("/api/crm/identity-verifications")
            .WithTags("CRM Identity Verification")
            .RequireAuthorization();

        _ = verification
            .MapPost("/issue", IssueAsync)
            .WithName("CrmIssueIdentityVerification")
            .WithSummary("Issue a rate-limited 6-digit identity verification challenge")
            .Produces<IdentityVerificationIssuedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = verification
            .MapPost("/{id:guid}/verify", VerifyAsync)
            .WithName("CrmVerifyIdentityCode")
            .WithSummary("Verify an identity verification code enforcing TTL and attempt limits")
            .Produces<IdentityVerificationResultResponse>(StatusCodes.Status200OK);

        var viewed = endpoints
            .MapGroup("/api/crm/person-viewed")
            .WithTags("CRM Person Viewed Audit")
            .RequireAuthorization();

        _ = viewed
            .MapPost("/", RecordViewedAsync)
            .WithName("CrmRecordPersonViewed")
            .WithSummary("Record a person profile view audit entry and increment ViewedCount")
            .Produces<PersonViewedResponse>(StatusCodes.Status201Created);

        _ = viewed
            .MapGet("/{targetPersonId:guid}", GetViewedHistoryAsync)
            .WithName("CrmGetPersonViewedHistory")
            .WithSummary("List recent profile view audit entries for a person")
            .Produces<IReadOnlyList<PersonViewedResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Created<IdentityVerificationIssuedResponse>, ProblemHttpResult>
    > IssueAsync(
        IssueIdentityVerificationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var ip = string.IsNullOrWhiteSpace(command.RequestIpAddress)
            ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            : command.RequestIpAddress;

        var result = await dispatcher.SendAsync<
            IssueIdentityVerificationCommand,
            IdentityVerificationIssuedResponse
        >(command with { RequestIpAddress = ip }, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created(
                new Uri(
                    $"/api/crm/identity-verifications/{result.Value.VerificationId:D}",
                    UriKind.Relative
                ),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IdentityVerificationResultResponse>, ProblemHttpResult>
    > VerifyAsync(
        Guid id,
        VerifyIdentityCodeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            VerifyIdentityCodeCommand,
            IdentityVerificationResultResponse
        >(command with { VerificationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<PersonViewedResponse>, ProblemHttpResult>
    > RecordViewedAsync(
        RecordPersonViewedCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var ip = command.IpAddress ?? httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await dispatcher.SendAsync<RecordPersonViewedCommand, PersonViewedResponse>(
            command with
            {
                IpAddress = ip,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/person-viewed/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonViewedResponse>>, ProblemHttpResult>
    > GetViewedHistoryAsync(
        Guid targetPersonId,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonViewedHistoryQuery,
            IReadOnlyList<PersonViewedResponse>
        >(new GetPersonViewedHistoryQuery(targetPersonId, maxResults ?? 50), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
