using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal static class DuplicateAndMergeEndpoints
{
    public static IEndpointRouteBuilder MapDuplicateAndMergeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/crm/duplicates")
            .WithTags("CRM Duplicates & Merge")
            .RequireAuthorization();

        _ = group
            .MapGet("/", GetDuplicatesAsync)
            .WithName("CrmGetPersonDuplicates")
            .WithSummary("List scored potential duplicate person pairs")
            .Produces<IReadOnlyList<PersonDuplicateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/scan", RunScanAsync)
            .WithName("CrmRunDuplicateScan")
            .WithSummary("Run an on-demand person duplicate detection scan")
            .Produces<DuplicateScanSummaryResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/{id:guid}/status", MarkStatusAsync)
            .WithName("CrmMarkDuplicateStatus")
            .WithSummary("Mark a duplicate pair as Not Duplicate or Ignore Until Score Changes")
            .Produces<PersonDuplicateResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/merge", MergeAsync)
            .WithName("CrmMergePeople")
            .WithSummary(
                "Merge a retired person into a surviving person and emit PersonMergedIntegrationEvent"
            )
            .Produces<PersonMergeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        _ = group
            .MapGet("/redirects/{personOrExternalId:guid}", ResolveRedirectAsync)
            .WithName("CrmResolveCanonicalPersonId")
            .WithSummary(
                "Resolve a retired PersonId or external record GUID to its canonical PersonId"
            )
            .Produces<CanonicalPersonResolutionResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonDuplicateResponse>>, ProblemHttpResult>
    > GetDuplicatesAsync(
        decimal? minConfidenceScore,
        bool? includeIgnored,
        bool? includeConfirmedNotDuplicate,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonDuplicatesQuery,
            IReadOnlyList<PersonDuplicateResponse>
        >(
            new GetPersonDuplicatesQuery(
                minConfidenceScore ?? 40m,
                includeIgnored ?? false,
                includeConfirmedNotDuplicate ?? false,
                maxResults ?? 100
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<DuplicateScanSummaryResponse>, ProblemHttpResult>
    > RunScanAsync(
        bool? includeInactive,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RunPersonDuplicateScanCommand,
            DuplicateScanSummaryResponse
        >(new RunPersonDuplicateScanCommand(includeInactive ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDuplicateResponse>, ProblemHttpResult>
    > MarkStatusAsync(
        Guid id,
        MarkDuplicateStatusCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            MarkDuplicateStatusCommand,
            PersonDuplicateResponse
        >(command with { DuplicateId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<PersonMergeResponse>, ProblemHttpResult>> MergeAsync(
        MergePeopleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<MergePeopleCommand, PersonMergeResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CanonicalPersonResolutionResponse>, ProblemHttpResult>
    > ResolveRedirectAsync(
        Guid personOrExternalId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            ResolveCanonicalPersonIdQuery,
            CanonicalPersonResolutionResponse
        >(new ResolveCanonicalPersonIdQuery(personOrExternalId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
