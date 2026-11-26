using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal static class EngagementJourneyEndpoints
{
    public static IEndpointRouteBuilder MapEngagementJourneyEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/engagement/journeys")
            .WithTags("Engagement Journeys & Milestones")
            .RequireAuthorization();

        _ = group
            .MapGet("/programs", ListProgramsAsync)
            .WithName("EngagementListJourneyPrograms")
            .WithSummary("List journey programs")
            .WithDescription("Returns milestone journey programs and prerequisite definitions.")
            .Produces<IReadOnlyList<JourneyProgramResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/programs", CreateProgramAsync)
            .WithName("EngagementCreateJourneyProgram")
            .WithSummary("Create journey program")
            .WithDescription(
                "Creates a journey program with validated acyclic milestone prerequisites."
            )
            .Produces<JourneyProgramResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/programs/{id:guid}", UpdateProgramAsync)
            .WithName("EngagementUpdateJourneyProgram")
            .WithSummary("Update journey program")
            .WithDescription("Updates statuses, milestone types, and completion rules.")
            .Produces<JourneyProgramResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        MapMilestoneProgressRoutes(group);
        return endpoints;
    }

    private static void MapMilestoneProgressRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/milestones", RecordMilestoneAsync)
            .WithName("EngagementRecordPersonMilestone")
            .WithSummary("Record or complete a person milestone")
            .WithDescription(
                "Validates prerequisites, records milestone completion, and evaluates program/achievement rollups."
            )
            .Produces<PersonMilestoneResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/people/{personId:guid}/progress", GetPersonProgressAsync)
            .WithName("EngagementGetPersonJourneyProgress")
            .WithSummary("Get person journey progress")
            .WithDescription("Returns constituent progress across active journey programs.")
            .Produces<IReadOnlyList<PersonJourneySummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/completions", ListCompletionsAsync)
            .WithName("EngagementListJourneyCompletions")
            .WithSummary("List journey program completions")
            .WithDescription("Returns completed journey programs filtered by program or person.")
            .Produces<IReadOnlyList<JourneyCompletionResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<JourneyProgramResponse>>, ProblemHttpResult>
    > ListProgramsAsync(bool? activeOnly, IQueryDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.QueryAsync<GetJourneyProgramsQuery, IReadOnlyList<JourneyProgramResponse>>(
            new GetJourneyProgramsQuery(activeOnly ?? false),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<JourneyProgramResponse>, ProblemHttpResult>
    > CreateProgramAsync(
        JourneyProgramSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateJourneyProgramCommand, JourneyProgramResponse>(
            new CreateJourneyProgramCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/journeys/programs/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<JourneyProgramResponse>, ProblemHttpResult>
    > UpdateProgramAsync(
        Guid id,
        JourneyProgramSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<UpdateJourneyProgramCommand, JourneyProgramResponse>(
            new UpdateJourneyProgramCommand(id, spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<PersonMilestoneResponse>, ProblemHttpResult>
    > RecordMilestoneAsync(
        RecordPersonMilestoneCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<RecordPersonMilestoneCommand, PersonMilestoneResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/journeys/milestones/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonJourneySummaryResponse>>, ProblemHttpResult>
    > GetPersonProgressAsync(
        Guid personId,
        Guid? journeyProgramId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetPersonJourneyProgressQuery,
            IReadOnlyList<PersonJourneySummaryResponse>
        >(new GetPersonJourneyProgressQuery(personId, journeyProgramId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<JourneyCompletionResponse>>, ProblemHttpResult>
    > ListCompletionsAsync(
        Guid? journeyProgramId,
        Guid? personId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetJourneyCompletionsQuery,
            IReadOnlyList<JourneyCompletionResponse>
        >(new GetJourneyCompletionsQuery(journeyProgramId, personId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
