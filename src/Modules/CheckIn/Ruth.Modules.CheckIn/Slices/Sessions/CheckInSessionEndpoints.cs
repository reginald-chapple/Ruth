using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal static class CheckInSessionEndpoints
{
    public static IEndpointRouteBuilder MapCheckInSessionEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        MapKioskSessionRoutes(endpoints);
        MapRoomManagerRoutes(endpoints);
        MapMobileCheckInRoutes(endpoints);

        return endpoints;
    }

    private static void MapKioskSessionRoutes(IEndpointRouteBuilder endpoints)
    {
        var sessions = endpoints
            .MapGroup("/api/checkin/sessions")
            .WithTags("Check-In Sessions & Kiosk")
            .RequireAuthorization();

        _ = sessions
            .MapPost("/search", SearchCandidatesAsync)
            .WithName("CheckInKioskSearch")
            .WithSummary("Search kiosk check-in candidates")
            .WithDescription(
                "Performs sub-50ms phone, barcode/card, or name lookup and evaluates area eligibility and live room balancing."
            )
            .Produces<KioskSearchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = sessions
            .MapPost("/", SubmitSessionAsync)
            .WithName("CheckInSubmitSession")
            .WithSummary("Submit a check-in session")
            .WithDescription(
                "Creates an atomic check-in session, allocates a collision-free daily security pickup code, and records room/group attendance."
            )
            .Produces<CheckInSessionReceiptResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = sessions
            .MapGet("/{id:guid}", GetSessionByIdAsync)
            .WithName("CheckInGetSessionById")
            .WithSummary("Get a check-in session receipt by ID")
            .WithDescription("Returns the completed check-in session receipt and entry statuses.")
            .Produces<CheckInSessionReceiptResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = sessions
            .MapGet("/active", LookupActiveAsync)
            .WithName("CheckInLookupActiveEntries")
            .WithSummary("Lookup active check-in entries for check-out")
            .WithDescription(
                "Finds actively checked-in entries by security pickup code, person ID, or household ID."
            )
            .Produces<IReadOnlyList<CheckedInEntryReceiptDto>>(StatusCodes.Status200OK);

        _ = sessions
            .MapPost("/checkout", CheckOutAsync)
            .WithName("CheckInCheckOutEntries")
            .WithSummary("Check out active participants")
            .WithDescription(
                "Checks out one or more active entries, verifying the guardian security pickup code unless staff-overridden."
            )
            .Produces<CheckOutResultResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapRoomManagerRoutes(IEndpointRouteBuilder endpoints)
    {
        var rooms = endpoints
            .MapGroup("/api/checkin/rooms")
            .WithTags("Check-In Room & Facility Manager")
            .RequireAuthorization();

        _ = rooms
            .MapGet("/roster", GetLiveRosterAsync)
            .WithName("CheckInGetLiveRoomRoster")
            .WithSummary("Get live room occupancy and participant rosters")
            .WithDescription(
                "Returns real-time room occupancy counts, threshold statuses, and checked-in rosters for the Room/Facility Manager tablet UI."
            )
            .Produces<LiveRoomRosterResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = rooms
            .MapPost("/override-assignment", OverrideRoomAssignmentAsync)
            .WithName("CheckInOverrideRoomAssignment")
            .WithSummary("Move or override a participant's room assignment")
            .WithDescription(
                "Reassigns an actively checked-in participant to another room/location with optional staff capacity override."
            )
            .Produces<CheckedInEntryReceiptDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapMobileCheckInRoutes(IEndpointRouteBuilder endpoints)
    {
        var mobile = endpoints
            .MapGroup("/api/checkin/mobile")
            .WithTags("Mobile Check-In")
            .RequireAuthorization();

        _ = mobile
            .MapPost("/search", SearchCandidatesAsync)
            .WithName("CheckInMobileSearch")
            .WithSummary("Search household check-in options from a mobile client")
            .WithDescription("Evaluates eligible areas and balanced rooms for mobile pre-check-in.")
            .Produces<KioskSearchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = mobile
            .MapPost("/sessions", SubmitMobileSessionAsync)
            .WithName("CheckInMobileSubmitSession")
            .WithSummary("Submit a mobile pre-check-in session")
            .WithDescription(
                "Submits a check-in session on the Mobile channel and issues an express barcode token and security pickup code."
            )
            .Produces<CheckInSessionReceiptResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<KioskSearchResponse>, ProblemHttpResult>
    > SearchCandidatesAsync(
        KioskSearchCandidatesQuery query,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<KioskSearchCandidatesQuery, KioskSearchResponse>(
            query,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CheckInSessionReceiptResponse>, ProblemHttpResult>
    > SubmitSessionAsync(
        SubmitCheckInSessionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var normalized = command with
        {
            ClientIpAddress = command.ClientIpAddress
                ?? httpContext.Connection.RemoteIpAddress?.ToString(),
        };
        var result = await dispatcher.SendAsync<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse
        >(normalized, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/checkin/sessions/{result.Value.SessionId:D}",
                result.Value
            )
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CheckInSessionReceiptResponse>, ProblemHttpResult>
    > SubmitMobileSessionAsync(
        SubmitCheckInSessionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        return await SubmitSessionAsync(
            command with
            {
                Channel = CheckInSessionChannel.Mobile,
            },
            dispatcher,
            httpContext,
            cancellationToken
        );
    }

    private static async Task<
        Results<Ok<CheckInSessionReceiptResponse>, ProblemHttpResult>
    > GetSessionByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCheckInSessionByIdQuery,
            CheckInSessionReceiptResponse
        >(new GetCheckInSessionByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CheckedInEntryReceiptDto>>, ProblemHttpResult>
    > LookupActiveAsync(
        Guid? configurationId,
        string? securityPickupCode,
        Guid? personId,
        Guid? householdId,
        DateOnly? serviceDate,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            LookupActiveCheckInsQuery,
            IReadOnlyList<CheckedInEntryReceiptDto>
        >(
            new LookupActiveCheckInsQuery(
                configurationId,
                securityPickupCode,
                personId,
                householdId,
                serviceDate
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<CheckOutResultResponse>, ProblemHttpResult>> CheckOutAsync(
        CheckOutAttendanceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CheckOutAttendanceCommand, CheckOutResultResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<LiveRoomRosterResponse>, ProblemHttpResult>
    > GetLiveRosterAsync(
        Guid configurationId,
        Guid? scheduleId,
        Guid? areaId,
        Guid? locationId,
        DateOnly? serviceDate,
        bool? includeCheckedOut,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetLiveRoomRosterQuery, LiveRoomRosterResponse>(
            new GetLiveRoomRosterQuery(
                configurationId,
                scheduleId,
                areaId,
                locationId,
                serviceDate,
                includeCheckedOut ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckedInEntryReceiptDto>, ProblemHttpResult>
    > OverrideRoomAssignmentAsync(
        OverrideCheckInRoomAssignmentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            OverrideCheckInRoomAssignmentCommand,
            CheckedInEntryReceiptDto
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
