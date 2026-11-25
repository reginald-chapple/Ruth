using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal static class RelationshipEndpoints
{
    public static IEndpointRouteBuilder MapRelationshipEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/crm/relationships")
            .WithTags("CRM Relationships & Graph")
            .RequireAuthorization();

        _ = group
            .MapGet("/known/{personId:guid}", GetKnownAsync)
            .WithName("CrmGetKnownRelationships")
            .WithSummary("List known interpersonal relationships for a person")
            .Produces<IReadOnlyList<KnownRelationshipResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/known", AddKnownAsync)
            .WithName("CrmAddKnownRelationship")
            .WithSummary("Create a known relationship and reciprocal inverse edge in Apache AGE")
            .Produces<KnownRelationshipResponse>(StatusCodes.Status201Created);

        _ = group
            .MapDelete("/known/{id:guid}", RemoveKnownAsync)
            .WithName("CrmRemoveKnownRelationship")
            .WithSummary("Remove a known relationship and its reciprocal inverse")
            .Produces(StatusCodes.Status204NoContent);

        _ = group
            .MapGet("/peer-network/{personId:guid}", GetPeerNetworkAsync)
            .WithName("CrmGetPeerNetwork")
            .WithSummary("List directed peer-network scores for a person")
            .Produces<IReadOnlyList<PeerNetworkEdgeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/peer-network", UpsertPeerNetworkAsync)
            .WithName("CrmUpsertPeerNetworkEdge")
            .WithSummary("Upsert a scored peer-network edge synchronized to Apache AGE")
            .Produces<PeerNetworkEdgeResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/graph/{personId:guid}", GetGraphAsync)
            .WithName("CrmGetRelationshipGraph")
            .WithSummary(
                "Traverse multi-hop family, known-relationship, and peer-network graph via Apache AGE"
            )
            .Produces<PersonRelationshipGraphResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<KnownRelationshipResponse>>, ProblemHttpResult>
    > GetKnownAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonKnownRelationshipsQuery,
            IReadOnlyList<KnownRelationshipResponse>
        >(new GetPersonKnownRelationshipsQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<KnownRelationshipResponse>, ProblemHttpResult>
    > AddKnownAsync(
        AddKnownRelationshipCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            AddKnownRelationshipCommand,
            KnownRelationshipResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/relationships/known/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveKnownAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveKnownRelationshipCommand, bool>(
            new RemoveKnownRelationshipCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PeerNetworkEdgeResponse>>, ProblemHttpResult>
    > GetPeerNetworkAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonPeerNetworkQuery,
            IReadOnlyList<PeerNetworkEdgeResponse>
        >(new GetPersonPeerNetworkQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PeerNetworkEdgeResponse>, ProblemHttpResult>
    > UpsertPeerNetworkAsync(
        UpsertPeerNetworkEdgeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpsertPeerNetworkEdgeCommand,
            PeerNetworkEdgeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonRelationshipGraphResponse>, ProblemHttpResult>
    > GetGraphAsync(
        Guid personId,
        int? maxHops,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonRelationshipGraphQuery,
            PersonRelationshipGraphResponse
        >(new GetPersonRelationshipGraphQuery(personId, maxHops ?? 2), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
