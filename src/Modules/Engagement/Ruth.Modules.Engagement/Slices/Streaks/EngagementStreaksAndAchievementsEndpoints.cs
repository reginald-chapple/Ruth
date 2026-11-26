using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal static class EngagementStreaksAndAchievementsEndpoints
{
    public static IEndpointRouteBuilder MapEngagementStreaksAndAchievementsEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var streaks = endpoints
            .MapGroup("/api/engagement/streaks")
            .WithTags("Engagement Streaks & Bitmask Cadence")
            .RequireAuthorization();

        _ = streaks
            .MapGet("/types", ListStreakTypesAsync)
            .WithName("EngagementListStreakTypes")
            .WithSummary("List streak types")
            .WithDescription("Returns configured daily and reporting-week streak types.")
            .Produces<IReadOnlyList<StreakTypeResponse>>(StatusCodes.Status200OK);

        _ = streaks
            .MapPost("/types", CreateStreakTypeAsync)
            .WithName("EngagementCreateStreakType")
            .WithSummary("Create streak type")
            .WithDescription("Creates a daily or reporting-week participation bitmask streak type.")
            .Produces<StreakTypeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = streaks
            .MapPost("/types/{id:guid}/exclusions", AddExclusionAsync)
            .WithName("EngagementAddStreakExclusion")
            .WithSummary("Add holiday or closure exclusion date")
            .WithDescription("Adds an exclusion date and recomputes enrolled streak metrics.")
            .Produces<StreakTypeResponse>(StatusCodes.Status200OK);

        _ = streaks
            .MapGet("/", ListStreaksAsync)
            .WithName("EngagementListStreaks")
            .WithSummary("Query constituent streaks")
            .WithDescription("Returns bitmask streak metrics and sparkline buckets.")
            .Produces<IReadOnlyList<StreakResponse>>(StatusCodes.Status200OK);

        _ = streaks
            .MapPost("/enroll", EnrollAsync)
            .WithName("EngagementEnrollInStreak")
            .WithSummary("Enroll person in streak")
            .WithDescription("Enrolls a constituent in a streak type.")
            .Produces<StreakResponse>(StatusCodes.Status201Created);

        _ = streaks
            .MapPost("/record", RecordEngagementAsync)
            .WithName("EngagementRecordStreakEngagement")
            .WithSummary("Record participation bit in streak")
            .WithDescription("Marks a participation bucket in the Span<byte> bitmask and evaluates achievements.")
            .Produces<StreakResponse>(StatusCodes.Status200OK);

        MapAchievementRoutes(endpoints);
        return endpoints;
    }

    private static void MapAchievementRoutes(IEndpointRouteBuilder endpoints)
    {
        var ach = endpoints
            .MapGroup("/api/engagement/achievements")
            .WithTags("Engagement Achievements & Badges")
            .RequireAuthorization();

        _ = ach
            .MapGet("/types", ListAchievementTypesAsync)
            .WithName("EngagementListAchievementTypes")
            .WithSummary("List achievement types")
            .WithDescription("Returns badge and milestone achievement definitions.")
            .Produces<IReadOnlyList<AchievementTypeResponse>>(StatusCodes.Status200OK);

        _ = ach
            .MapPost("/types", CreateAchievementTypeAsync)
            .WithName("EngagementCreateAchievementType")
            .WithSummary("Create achievement type")
            .WithDescription("Creates an event-driven badge or certification milestone definition.")
            .Produces<AchievementTypeResponse>(StatusCodes.Status201Created);

        _ = ach
            .MapGet("/attempts", ListAttemptsAsync)
            .WithName("EngagementListAchievementAttempts")
            .WithSummary("Query achievement attempts")
            .WithDescription("Returns in-progress and earned achievement attempts.")
            .Produces<IReadOnlyList<AchievementAttemptResponse>>(StatusCodes.Status200OK);

        _ = ach
            .MapPost("/progress", RecordAchievementProgressAsync)
            .WithName("EngagementRecordAchievementProgress")
            .WithSummary("Record achievement progress")
            .WithDescription("Evaluates prerequisites, updates attempt progress, and awards badges.")
            .Produces<AchievementAttemptResponse>(StatusCodes.Status200OK);
    }

    private static async Task<Results<Ok<IReadOnlyList<StreakTypeResponse>>, ProblemHttpResult>> ListStreakTypesAsync(
        bool? activeOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetStreakTypesQuery, IReadOnlyList<StreakTypeResponse>>(
            new GetStreakTypesQuery(activeOnly ?? false),
            ct
        );
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Created<StreakTypeResponse>, ProblemHttpResult>> CreateStreakTypeAsync(
        StreakTypeSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateStreakTypeCommand, StreakTypeResponse>(
            new CreateStreakTypeCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/streaks/types/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<StreakTypeResponse>, ProblemHttpResult>> AddExclusionAsync(
        Guid id,
        AddStreakExclusionCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<AddStreakExclusionCommand, StreakTypeResponse>(
            body with { StreakTypeId = id },
            ct
        );
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<IReadOnlyList<StreakResponse>>, ProblemHttpResult>> ListStreaksAsync(
        Guid? streakTypeId,
        Guid? personId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetStreaksQuery, IReadOnlyList<StreakResponse>>(
            new GetStreaksQuery(streakTypeId, personId),
            ct
        );
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Created<StreakResponse>, ProblemHttpResult>> EnrollAsync(
        EnrollInStreakCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<EnrollInStreakCommand, StreakResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/streaks/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<StreakResponse>, ProblemHttpResult>> RecordEngagementAsync(
        RecordStreakEngagementCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<RecordStreakEngagementCommand, StreakResponse>(body, ct);
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<IReadOnlyList<AchievementTypeResponse>>, ProblemHttpResult>> ListAchievementTypesAsync(
        bool? activeOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetAchievementTypesQuery, IReadOnlyList<AchievementTypeResponse>>(
            new GetAchievementTypesQuery(activeOnly ?? false),
            ct
        );
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Created<AchievementTypeResponse>, ProblemHttpResult>> CreateAchievementTypeAsync(
        AchievementTypeSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateAchievementTypeCommand, AchievementTypeResponse>(
            new CreateAchievementTypeCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/achievements/types/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<IReadOnlyList<AchievementAttemptResponse>>, ProblemHttpResult>> ListAttemptsAsync(
        Guid? achievementTypeId,
        Guid? personId,
        bool? successfulOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetAchievementAttemptsQuery, IReadOnlyList<AchievementAttemptResponse>>(
            new GetAchievementAttemptsQuery(achievementTypeId, personId, successfulOnly ?? false),
            ct
        );
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<AchievementAttemptResponse>, ProblemHttpResult>> RecordAchievementProgressAsync(
        RecordAchievementProgressCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<RecordAchievementProgressCommand, AchievementAttemptResponse>(body, ct);
        return r.IsSuccess ? TypedResults.Ok(r.Value) : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
