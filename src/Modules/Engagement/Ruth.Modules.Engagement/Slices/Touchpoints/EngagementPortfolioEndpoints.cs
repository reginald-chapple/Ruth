using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal static class EngagementPortfolioEndpoints
{
    public static IEndpointRouteBuilder MapEngagementPortfolioEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/engagement/portfolio")
            .WithTags("Engagement Constituent Portfolio & Touchpoints")
            .RequireAuthorization();

        _ = group
            .MapGet("/queue", GetQueueAsync)
            .WithName("EngagementGetPortfolioOutreachQueue")
            .WithSummary("Get Mobile/Web My Portfolio & Outreach Queue")
            .WithDescription(
                "Returns relationship health KPIs, prioritized due touchpoints, and portfolio contacts."
            )
            .Produces<PortfolioOutreachQueueResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/contacts", UpsertContactAsync)
            .WithName("EngagementUpsertConstituentContact")
            .WithSummary("Add or update constituent portfolio contact")
            .WithDescription(
                "Upserts a constituent in a steward/caseworker portfolio and seeds initial cadence."
            )
            .Produces<ConstituentContactResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPost("/touchpoints", ScheduleTouchpointAsync)
            .WithName("EngagementScheduleTouchpoint")
            .WithSummary("Schedule ad-hoc or follow-up touchpoint")
            .WithDescription("Schedules an outreach touchpoint for a portfolio contact.")
            .Produces<TouchpointResponse>(StatusCodes.Status201Created);

        MapTouchpointActionRoutes(group);
        return endpoints;
    }

    private static void MapTouchpointActionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/touchpoints/{id:guid}/complete", CompleteTouchpointAsync)
            .WithName("EngagementCompleteTouchpoint")
            .WithSummary("Complete outreach touchpoint")
            .WithDescription(
                "Logs outreach completion, updates relationship health, schedules next cycle, and syncs CRM peer network."
            )
            .Produces<TouchpointResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/touchpoints/{id:guid}/snooze", SnoozeTouchpointAsync)
            .WithName("EngagementSnoozeTouchpoint")
            .WithSummary("Snooze outreach touchpoint")
            .WithDescription("Defers a pending touchpoint to a later calendar date.")
            .Produces<TouchpointResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/touchpoints/{id:guid}/skip", SkipTouchpointAsync)
            .WithName("EngagementSkipTouchpoint")
            .WithSummary("Skip outreach touchpoint")
            .WithDescription(
                "Skips the current cycle touchpoint and advances to the next cadence window."
            )
            .Produces<TouchpointResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/generate-cadence", GenerateCadenceAsync)
            .WithName("EngagementGeneratePortfolioCadence")
            .WithSummary("Generate due and upcoming cadence touchpoints")
            .WithDescription(
                "Evaluates active portfolios and schedules due outreach and connection-note touchpoints."
            )
            .Produces<TouchpointCadenceRunResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<PortfolioOutreachQueueResponse>, ProblemHttpResult>
    > GetQueueAsync(
        Guid? ownerPersonId,
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetMyPortfolioOutreachQueueQuery,
            PortfolioOutreachQueueResponse
        >(new GetMyPortfolioOutreachQueueQuery(ownerPersonId, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<ConstituentContactResponse>, ProblemHttpResult>
    > UpsertContactAsync(
        ConstituentContactSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<UpsertConstituentContactCommand, ConstituentContactResponse>(
            new UpsertConstituentContactCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/portfolio/contacts/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<TouchpointResponse>, ProblemHttpResult>
    > ScheduleTouchpointAsync(
        ScheduleTouchpointCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ScheduleTouchpointCommand, TouchpointResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/portfolio/touchpoints/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<TouchpointResponse>, ProblemHttpResult>
    > CompleteTouchpointAsync(
        Guid id,
        CompleteTouchpointCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CompleteTouchpointCommand, TouchpointResponse>(
            body with
            {
                TouchpointId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<TouchpointResponse>, ProblemHttpResult>
    > SnoozeTouchpointAsync(
        Guid id,
        SnoozeTouchpointCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<SnoozeTouchpointCommand, TouchpointResponse>(
            body with
            {
                TouchpointId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<TouchpointResponse>, ProblemHttpResult>
    > SkipTouchpointAsync(
        Guid id,
        SkipTouchpointCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<SkipTouchpointCommand, TouchpointResponse>(
            body with
            {
                TouchpointId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<TouchpointCadenceRunResponse>, ProblemHttpResult>
    > GenerateCadenceAsync(
        GeneratePortfolioCadenceTouchpointsCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<
            GeneratePortfolioCadenceTouchpointsCommand,
            TouchpointCadenceRunResponse
        >(body, ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
