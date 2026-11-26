using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Slices.Calendars;

internal static class EventCalendarEndpoints
{
    public static IEndpointRouteBuilder MapEventCalendarEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var calendarsGroup = endpoints
            .MapGroup("/api/events/calendars")
            .WithTags("Events Calendars & Feeds")
            .RequireAuthorization();

        MapCalendarAdminRoutes(calendarsGroup);

        var itemsGroup = endpoints
            .MapGroup("/api/events/items")
            .WithTags("Events Calendars & Feeds")
            .RequireAuthorization();

        MapEventItemRoutes(itemsGroup);

        _ = endpoints
            .MapGet("/api/events/calendars/{slugOrId}/feed.ics", DownloadIcsFeedAsync)
            .WithTags("Events Calendars & Feeds")
            .AllowAnonymous()
            .WithName("EventsDownloadCalendarIcsFeed")
            .WithSummary("Download RFC 5545 iCalendar (.ics) feed for an event calendar")
            .WithDescription(
                "Compiles approved event occurrences and schedule expansions into an RFC 5545 VCALENDAR 2.0 feed."
            )
            .Produces(StatusCodes.Status200OK, contentType: "text/calendar")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static void MapCalendarAdminRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListCalendarsAsync)
            .WithName("EventsListCalendars")
            .WithSummary("List event calendars")
            .WithDescription("Returns configured public and internal event calendars.")
            .Produces<IReadOnlyList<EventCalendarResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateCalendarAsync)
            .WithName("EventsCreateCalendar")
            .WithSummary("Create an event calendar")
            .WithDescription("Creates a new event calendar with unique URL slug.")
            .Produces<EventCalendarResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateCalendarAsync)
            .WithName("EventsUpdateCalendar")
            .WithSummary("Update an event calendar")
            .WithDescription("Updates calendar name, slug, visibility, and active status.")
            .Produces<EventCalendarResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapEventItemRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListEventItemsAsync)
            .WithName("EventsListEventItems")
            .WithSummary("List event items")
            .WithDescription(
                "Lists event items filtered optionally by calendar, branch, or audience."
            )
            .Produces<IReadOnlyList<EventItemResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetEventItemByIdAsync)
            .WithName("EventsGetEventItemById")
            .WithSummary("Get an event item by identifier")
            .WithDescription("Returns an event item with calendar memberships and occurrences.")
            .Produces<EventItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateEventItemAsync)
            .WithName("EventsCreateEventItem")
            .WithSummary("Create an event item")
            .WithDescription("Creates an event item linked to one or more calendars and audiences.")
            .Produces<EventItemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateEventItemAsync)
            .WithName("EventsUpdateEventItem")
            .WithSummary("Update an event item")
            .WithDescription("Updates event item metadata, calendars, and audiences.")
            .Produces<EventItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/occurrences", AddOccurrenceAsync)
            .WithName("EventsAddEventItemOccurrence")
            .WithSummary("Add an occurrence to an event item")
            .WithDescription(
                "Attaches a Branch, Location, and Schedule or explicit UTC window occurrence."
            )
            .Produces<EventItemOccurrenceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}/occurrences/{occurrenceId:guid}", UpdateOccurrenceAsync)
            .WithName("EventsUpdateEventItemOccurrence")
            .WithSummary("Update an event item occurrence")
            .WithDescription(
                "Updates occurrence schedule, branch, location, or cancellation state."
            )
            .Produces<EventItemOccurrenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EventCalendarResponse>>, ProblemHttpResult>
    > ListCalendarsAsync(
        bool? publicOnly,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetEventCalendarsQuery,
            IReadOnlyList<EventCalendarResponse>
        >(new GetEventCalendarsQuery(publicOnly ?? false, activeOnly ?? false), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<EventCalendarResponse>, ProblemHttpResult>
    > CreateCalendarAsync(
        EventCalendarSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<CreateEventCalendarCommand, EventCalendarResponse>(
            new CreateEventCalendarCommand(spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created($"/api/events/calendars/{res.Value.Id:D}", res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<EventCalendarResponse>, ProblemHttpResult>
    > UpdateCalendarAsync(
        Guid id,
        EventCalendarSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<UpdateEventCalendarCommand, EventCalendarResponse>(
            new UpdateEventCalendarCommand(id, spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EventItemResponse>>, ProblemHttpResult>
    > ListEventItemsAsync(
        Guid? calendarId,
        Guid? branchId,
        Guid? audienceDefinedValueId,
        bool? activeOnly,
        bool? publicApprovedOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetEventItemsQuery, IReadOnlyList<EventItemResponse>>(
            new GetEventItemsQuery(
                calendarId,
                branchId,
                audienceDefinedValueId,
                activeOnly ?? false,
                publicApprovedOnly ?? false
            ),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<EventItemResponse>, ProblemHttpResult>
    > GetEventItemByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetEventItemByIdQuery, EventItemResponse>(
            new GetEventItemByIdQuery(id),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<EventItemResponse>, ProblemHttpResult>
    > CreateEventItemAsync(
        EventItemSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<CreateEventItemCommand, EventItemResponse>(
            new CreateEventItemCommand(spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created($"/api/events/items/{res.Value.Id:D}", res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<EventItemResponse>, ProblemHttpResult>
    > UpdateEventItemAsync(
        Guid id,
        EventItemSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<UpdateEventItemCommand, EventItemResponse>(
            new UpdateEventItemCommand(id, spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<EventItemOccurrenceResponse>, ProblemHttpResult>
    > AddOccurrenceAsync(
        Guid id,
        EventItemOccurrenceSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            AddEventItemOccurrenceCommand,
            EventItemOccurrenceResponse
        >(new AddEventItemOccurrenceCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/events/items/{id:D}/occurrences/{res.Value.Id:D}",
                res.Value
            )
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<EventItemOccurrenceResponse>, ProblemHttpResult>
    > UpdateOccurrenceAsync(
        Guid id,
        Guid occurrenceId,
        EventItemOccurrenceSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateEventItemOccurrenceCommand,
            EventItemOccurrenceResponse
        >(new UpdateEventItemOccurrenceCommand(id, occurrenceId, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<IResult> DownloadIcsFeedAsync(
        string slugOrId,
        DateTimeOffset? rangeStartUtc,
        DateTimeOffset? rangeEndUtc,
        Guid? branchId,
        Guid? audienceDefinedValueId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetCalendarIcsFeedQuery, CalendarFeedPayloadResponse>(
            new GetCalendarIcsFeedQuery(
                slugOrId,
                rangeStartUtc,
                rangeEndUtc,
                branchId,
                audienceDefinedValueId
            ),
            cancellationToken
        );
        return res.IsFailure
            ? EventsProblemResultMapper.ToProblemResult(res, httpContext)
            : TypedResults.Text(
                res.Value.IcsContent,
                contentType: "text/calendar; charset=utf-8",
                contentEncoding: Encoding.UTF8
            );
    }
}
