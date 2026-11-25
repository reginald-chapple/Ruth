using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal static class CheckInConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapCheckInConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/checkin/configurations")
            .WithTags("Check-In Configurations")
            .RequireAuthorization();

        MapProfileEndpoints(group);
        MapAreaAndEvaluationEndpoints(group);

        return endpoints;
    }

    private static void MapProfileEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("CheckInListConfigurations")
            .WithSummary("List check-in configuration profiles")
            .WithDescription(
                "Returns check-in configuration summaries filtered optionally by branch and active status."
            )
            .Produces<IReadOnlyList<CheckInConfigurationSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CheckInGetConfigurationById")
            .WithSummary("Get check-in configuration profile by ID")
            .WithDescription(
                "Returns full check-in configuration details including areas, eligibility rules, and room matrices."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CheckInCreateConfiguration")
            .WithSummary("Create a check-in configuration profile")
            .WithDescription(
                "Creates a new check-in configuration profile for a facility, program, youth, shift, or event scenario."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CheckInUpdateConfiguration")
            .WithSummary("Update a check-in configuration profile")
            .WithDescription(
                "Updates scalar settings on an existing check-in configuration profile."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapAreaAndEvaluationEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/areas", UpsertAreaAsync)
            .WithName("CheckInUpsertArea")
            .WithSummary("Add or update a check-in area and room matrix")
            .WithDescription(
                "Creates or updates an area, its participant eligibility rules, and its room/schedule/capacity matrix."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}/areas/{areaId:guid}", RemoveAreaAsync)
            .WithName("CheckInRemoveArea")
            .WithSummary("Remove a check-in area")
            .WithDescription(
                "Removes an area and its room matrix from a check-in configuration profile."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPatch(
                "/{id:guid}/areas/{areaId:guid}/rooms/{locationId:guid}/availability",
                SetRoomAvailabilityAsync
            )
            .WithName("CheckInSetRoomAvailability")
            .WithSummary("Update room open/closed state and capacity thresholds")
            .WithDescription(
                "Toggles live room availability and soft/firm capacity limits on an area matrix entry."
            )
            .Produces<CheckInConfigurationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/evaluate", EvaluateOptionsAsync)
            .WithName("CheckInEvaluateOptions")
            .WithSummary("Evaluate participant eligibility and room balancing")
            .WithDescription(
                "Evaluates eligible areas and balanced room recommendations for a participant context and schedule."
            )
            .Produces<CheckInEvaluationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CheckInConfigurationSummaryResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? branchId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>
        >(new GetCheckInConfigurationsQuery(branchId, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCheckInConfigurationByIdQuery,
            CheckInConfigurationDetailResponse
        >(new GetCheckInConfigurationByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateCheckInConfigurationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/checkin/configurations/{result.Value.Id:D}", result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateCheckInConfigurationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(command with { ConfigurationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > UpsertAreaAsync(
        Guid id,
        UpsertCheckInAreaCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(command with { ConfigurationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > RemoveAreaAsync(
        Guid id,
        Guid areaId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RemoveCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(new RemoveCheckInAreaCommand(id, areaId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInConfigurationDetailResponse>, ProblemHttpResult>
    > SetRoomAvailabilityAsync(
        Guid id,
        Guid areaId,
        Guid locationId,
        SetRoomCheckInAvailabilityCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var normalized = command with
        {
            ConfigurationId = id,
            AreaId = areaId,
            LocationId = locationId,
        };
        var result = await dispatcher.SendAsync<
            SetRoomCheckInAvailabilityCommand,
            CheckInConfigurationDetailResponse
        >(normalized, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInEvaluationResponse>, ProblemHttpResult>
    > EvaluateOptionsAsync(
        Guid id,
        EvaluateCheckInOptionsQuery query,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            EvaluateCheckInOptionsQuery,
            CheckInEvaluationResponse
        >(query with { ConfigurationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
