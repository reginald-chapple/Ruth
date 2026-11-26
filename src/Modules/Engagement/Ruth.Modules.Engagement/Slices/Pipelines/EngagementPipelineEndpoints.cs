using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal static class EngagementPipelineEndpoints
{
    public static IEndpointRouteBuilder MapEngagementPipelineEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/engagement/pipelines")
            .WithTags("Engagement Pipelines & Connector Board")
            .RequireAuthorization();

        MapPipelineSetupRoutes(group);
        EngagementConnectorBoardEndpoints.MapRequestAndBoardRoutes(group);
        _ = endpoints.MapEngagementPipelinePublicEndpoints();
        return endpoints;
    }

    private static void MapPipelineSetupRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListPipelinesAsync)
            .WithName("EngagementListPipelineTypes")
            .WithSummary("List engagement pipeline types")
            .WithDescription(
                "Returns configured volunteer, onboarding, mentorship, and referral pipelines."
            )
            .Produces<IReadOnlyList<PipelineTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreatePipelineAsync)
            .WithName("EngagementCreatePipelineType")
            .WithSummary("Create engagement pipeline type")
            .WithDescription(
                "Creates a pipeline with ordered stages, idle thresholds, and stage automations."
            )
            .Produces<PipelineTypeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdatePipelineAsync)
            .WithName("EngagementUpdatePipelineType")
            .WithSummary("Update engagement pipeline type")
            .WithDescription("Updates stages, activity types, and progression rules.")
            .Produces<PipelineTypeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/opportunities", ListOpportunitiesAsync)
            .WithName("EngagementListStaffOpportunities")
            .WithSummary("List engagement opportunities")
            .WithDescription("Returns intake opportunities filtered by pipeline or branch.")
            .Produces<IReadOnlyList<EngagementOpportunityResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/opportunities", CreateOpportunityAsync)
            .WithName("EngagementCreateOpportunity")
            .WithSummary("Create engagement opportunity")
            .WithDescription(
                "Creates a branch/group/coordinator-configured engagement opportunity."
            )
            .Produces<EngagementOpportunityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/opportunities/{id:guid}", UpdateOpportunityAsync)
            .WithName("EngagementUpdateOpportunity")
            .WithSummary("Update engagement opportunity")
            .WithDescription("Updates opportunity routing, placement groups, and visibility.")
            .Produces<EngagementOpportunityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PipelineTypeResponse>>, ProblemHttpResult>
    > ListPipelinesAsync(
        bool? activeOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetPipelineTypesQuery, IReadOnlyList<PipelineTypeResponse>>(
            new GetPipelineTypesQuery(activeOnly ?? false),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<PipelineTypeResponse>, ProblemHttpResult>
    > CreatePipelineAsync(
        PipelineTypeSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreatePipelineTypeCommand, PipelineTypeResponse>(
            new CreatePipelineTypeCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/pipelines/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<PipelineTypeResponse>, ProblemHttpResult>
    > UpdatePipelineAsync(
        Guid id,
        PipelineTypeSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<UpdatePipelineTypeCommand, PipelineTypeResponse>(
            new UpdatePipelineTypeCommand(id, spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EngagementOpportunityResponse>>, ProblemHttpResult>
    > ListOpportunitiesAsync(
        Guid? pipelineTypeId,
        Guid? branchId,
        bool? activeOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetEngagementOpportunitiesQuery,
            IReadOnlyList<EngagementOpportunityResponse>
        >(
            new GetEngagementOpportunitiesQuery(
                pipelineTypeId,
                branchId,
                activeOnly ?? false,
                PublicOnly: false
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<EngagementOpportunityResponse>, ProblemHttpResult>
    > CreateOpportunityAsync(
        EngagementOpportunitySpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<
            CreateEngagementOpportunityCommand,
            EngagementOpportunityResponse
        >(new CreateEngagementOpportunityCommand(spec), ct);
        return r.IsSuccess
            ? TypedResults.Created(
                $"/api/engagement/pipelines/opportunities/{r.Value.Id:D}",
                r.Value
            )
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementOpportunityResponse>, ProblemHttpResult>
    > UpdateOpportunityAsync(
        Guid id,
        EngagementOpportunitySpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<
            UpdateEngagementOpportunityCommand,
            EngagementOpportunityResponse
        >(new UpdateEngagementOpportunityCommand(id, spec), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
