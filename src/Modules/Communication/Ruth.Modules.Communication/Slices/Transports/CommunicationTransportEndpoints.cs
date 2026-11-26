using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Transports;

internal static class CommunicationTransportEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationTransportEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/transports")
            .WithTags("Communication Transports")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListTransportsAsync)
            .WithName("CommunicationListTransports")
            .WithSummary(
                "List registered communication transport providers and active channel defaults"
            )
            .Produces<IReadOnlyList<CommunicationTransportStatusResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/in-memory-messages", ListInMemoryMessagesAsync)
            .WithName("CommunicationListInMemoryMessages")
            .WithSummary("Inspect messages captured by the dev in-memory sink and push stub")
            .Produces<IReadOnlyList<InMemoryCapturedMessageResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommunicationTransportStatusResponse>>, ProblemHttpResult>
    > ListTransportsAsync(
        CommunicationChannel? channel,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationTransportsQuery,
            IReadOnlyList<CommunicationTransportStatusResponse>
        >(new GetCommunicationTransportsQuery(channel), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<InMemoryCapturedMessageResponse>>, ProblemHttpResult>
    > ListInMemoryMessagesAsync(
        CommunicationChannel? channel,
        string? recipientFilter,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetInMemorySentMessagesQuery,
            IReadOnlyList<InMemoryCapturedMessageResponse>
        >(
            new GetInMemorySentMessagesQuery(channel, recipientFilter, limit ?? 50),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
