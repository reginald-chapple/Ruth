using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Schedules;

internal static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/schedules")
            .WithTags("Schedules")
            .RequireAuthorization();

        MapScheduleRoutes(group);
        MapExclusionRoutes(group);
        return endpoints;
    }

    private static void MapScheduleRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", GetAllAsync)
            .WithName("CoreGetSchedules")
            .WithSummary("List schedules with optional category and active filters")
            .Produces<IReadOnlyList<ScheduleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetScheduleById")
            .WithSummary("Get schedule details")
            .Produces<ScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/{id:guid}/occurrences", GetOccurrencesAsync)
            .WithName("CoreGetScheduleOccurrences")
            .WithSummary("Evaluate concrete schedule occurrences over a UTC range")
            .Produces<IReadOnlyList<ScheduleOccurrenceDto>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateSchedule")
            .WithSummary("Create a new schedule and materialize its ScheduleDate horizon")
            .Produces<ScheduleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateSchedule")
            .WithSummary("Update an existing schedule and refresh materialized dates")
            .Produces<ScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteSchedule")
            .WithSummary("Delete a schedule")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapExclusionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/categories/{categoryId:guid}/exclusions", GetExclusionsAsync)
            .WithName("CoreGetScheduleCategoryExclusions")
            .WithSummary("List blackout exclusion date ranges for a schedule category")
            .Produces<IReadOnlyList<ScheduleCategoryExclusionResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/categories/{categoryId:guid}/exclusions", CreateExclusionAsync)
            .WithName("CoreCreateScheduleCategoryExclusion")
            .WithSummary("Add a schedule category blackout exclusion")
            .Produces<ScheduleCategoryExclusionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapDelete("/exclusions/{exclusionId:guid}", DeleteExclusionAsync)
            .WithName("CoreDeleteScheduleCategoryExclusion")
            .WithSummary("Delete a schedule category blackout exclusion")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ScheduleResponse>>, ProblemHttpResult>
    > GetAllAsync(
        Guid? categoryId,
        bool? activeOnly,
        string? search,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSchedulesQuery,
            IReadOnlyList<ScheduleResponse>
        >(new GetSchedulesQuery(categoryId, activeOnly ?? false, search), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<ScheduleResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetScheduleByIdQuery, ScheduleResponse>(
            new GetScheduleByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ScheduleOccurrenceDto>>, ProblemHttpResult>
    > GetOccurrencesAsync(
        Guid id,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? timeZoneId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetScheduleOccurrencesQuery,
            IReadOnlyList<ScheduleOccurrenceDto>
        >(new GetScheduleOccurrencesQuery(id, startUtc, endUtc, timeZoneId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<ScheduleResponse>, ProblemHttpResult>> CreateAsync(
        CreateScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateScheduleCommand, ScheduleResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/schedules/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<ScheduleResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateScheduleCommand, ScheduleResponse>(
            command with
            {
                ScheduleId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteScheduleCommand, bool>(
            new DeleteScheduleCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ScheduleCategoryExclusionResponse>>, ProblemHttpResult>
    > GetExclusionsAsync(
        Guid categoryId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetScheduleCategoryExclusionsQuery,
            IReadOnlyList<ScheduleCategoryExclusionResponse>
        >(new GetScheduleCategoryExclusionsQuery(categoryId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<ScheduleCategoryExclusionResponse>, ProblemHttpResult>
    > CreateExclusionAsync(
        Guid categoryId,
        CreateScheduleCategoryExclusionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateScheduleCategoryExclusionCommand,
            ScheduleCategoryExclusionResponse
        >(command with { CategoryId = categoryId }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/schedules/exclusions/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteExclusionAsync(
        Guid exclusionId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteScheduleCategoryExclusionCommand, bool>(
            new DeleteScheduleCategoryExclusionCommand(exclusionId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
