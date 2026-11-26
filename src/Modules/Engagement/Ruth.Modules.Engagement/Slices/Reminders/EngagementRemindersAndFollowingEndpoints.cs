using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal static class EngagementRemindersAndFollowingEndpoints
{
    public static IEndpointRouteBuilder MapEngagementRemindersAndFollowingEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var rem = endpoints
            .MapGroup("/api/engagement/reminders")
            .WithTags("Engagement Reminders, Notifications & Following")
            .RequireAuthorization();

        _ = rem.MapGet("/types", ListTypesAsync)
            .WithName("EngagementListReminderTypes")
            .WithSummary("List reminder types")
            .WithDescription("Returns configured entity follow-up reminder types.")
            .Produces<IReadOnlyList<ReminderTypeResponse>>(StatusCodes.Status200OK);

        _ = rem.MapPost("/types", CreateTypeAsync)
            .WithName("EngagementCreateReminderType")
            .WithSummary("Create reminder type")
            .WithDescription("Creates an entity follow-up reminder type.")
            .Produces<ReminderTypeResponse>(StatusCodes.Status201Created);

        _ = rem.MapGet("/", ListRemindersAsync)
            .WithName("EngagementListReminders")
            .WithSummary("List entity reminders")
            .WithDescription("Returns active or completed follow-up reminders.")
            .Produces<IReadOnlyList<ReminderResponse>>(StatusCodes.Status200OK);

        _ = rem.MapPost("/", CreateReminderAsync)
            .WithName("EngagementCreateReminder")
            .WithSummary("Create entity follow-up reminder")
            .WithDescription("Creates a one-shot or cyclic entity follow-up reminder.")
            .Produces<ReminderResponse>(StatusCodes.Status201Created);

        _ = rem.MapPost("/{id:guid}/complete", CompleteReminderAsync)
            .WithName("EngagementCompleteReminder")
            .WithSummary("Complete reminder")
            .WithDescription("Marks an entity follow-up reminder complete.")
            .Produces<ReminderResponse>(StatusCodes.Status200OK);

        _ = rem.MapPost("/process-due", ProcessDueAsync)
            .WithName("EngagementProcessDueReminders")
            .WithSummary("Process due reminders")
            .WithDescription("Evaluates due reminders and generates Notification Bell items.")
            .Produces<ReminderProcessRunResponse>(StatusCodes.Status200OK);

        MapNotificationAndFollowingRoutes(endpoints);
        return endpoints;
    }

    private static void MapNotificationAndFollowingRoutes(IEndpointRouteBuilder endpoints)
    {
        var notif = endpoints
            .MapGroup("/api/engagement/notifications")
            .WithTags("Engagement Reminders, Notifications & Following")
            .RequireAuthorization();

        _ = notif
            .MapGet("/", ListNotificationsAsync)
            .WithName("EngagementListNotifications")
            .WithSummary("List Notification Bell messages")
            .WithDescription("Returns unread or recent in-app notifications.")
            .Produces<IReadOnlyList<NotificationMessageResponse>>(StatusCodes.Status200OK);

        _ = notif
            .MapPost("/{id:guid}/read", MarkReadAsync)
            .WithName("EngagementMarkNotificationRead")
            .WithSummary("Mark notification read")
            .WithDescription("Marks an in-app notification message read.")
            .Produces<NotificationMessageResponse>(StatusCodes.Status200OK);

        var fol = endpoints
            .MapGroup("/api/engagement/following")
            .WithTags("Engagement Reminders, Notifications & Following")
            .RequireAuthorization();

        _ = fol.MapGet("/{personId:guid}", ListFollowingsAsync)
            .WithName("EngagementListPersonFollowings")
            .WithSummary("List followed entities for person")
            .WithDescription("Returns active entity subscriptions for a follower.")
            .Produces<IReadOnlyList<FollowingSubscriptionResponse>>(StatusCodes.Status200OK);

        _ = fol.MapPost("/toggle", ToggleFollowingAsync)
            .WithName("EngagementToggleFollowing")
            .WithSummary("Subscribe or unsubscribe from entity following")
            .WithDescription("Toggles a following subscription for digest notifications.")
            .Produces<FollowingSubscriptionResponse>(StatusCodes.Status200OK);

        _ = fol.MapPost("/events", RecordFollowedEventAsync)
            .WithName("EngagementRecordFollowedEntityEvent")
            .WithSummary("Record notable event on followed entity")
            .WithDescription("Stages an entity event for the next following digest run.")
            .Produces<Guid>(StatusCodes.Status201Created);

        _ = fol.MapPost("/dispatch-digests", DispatchDigestsAsync)
            .WithName("EngagementDispatchFollowingDigests")
            .WithSummary("Dispatch consolidated following digests")
            .WithDescription(
                "Consolidates pending followed-entity events into digest notifications."
            )
            .Produces<FollowingDigestRunResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ReminderTypeResponse>>, ProblemHttpResult>
    > ListTypesAsync(bool? activeOnly, IQueryDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.QueryAsync<GetReminderTypesQuery, IReadOnlyList<ReminderTypeResponse>>(
            new GetReminderTypesQuery(activeOnly ?? false),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<ReminderTypeResponse>, ProblemHttpResult>
    > CreateTypeAsync(
        ReminderTypeSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateReminderTypeCommand, ReminderTypeResponse>(
            new CreateReminderTypeCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/reminders/types/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ReminderResponse>>, ProblemHttpResult>
    > ListRemindersAsync(
        Guid? assigneePersonId,
        string? entityTypeKey,
        Guid? entityId,
        bool? includeCompleted,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetRemindersQuery, IReadOnlyList<ReminderResponse>>(
            new GetRemindersQuery(
                assigneePersonId,
                entityTypeKey,
                entityId,
                includeCompleted ?? false
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<ReminderResponse>, ProblemHttpResult>
    > CreateReminderAsync(
        CreateReminderCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateReminderCommand, ReminderResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/reminders/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<ReminderResponse>, ProblemHttpResult>
    > CompleteReminderAsync(Guid id, ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<CompleteReminderCommand, ReminderResponse>(
            new CompleteReminderCommand(id),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<ReminderProcessRunResponse>, ProblemHttpResult>
    > ProcessDueAsync(ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<ProcessDueRemindersCommand, ReminderProcessRunResponse>(
            new ProcessDueRemindersCommand(),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<NotificationMessageResponse>>, ProblemHttpResult>
    > ListNotificationsAsync(
        Guid? recipientPersonId,
        bool? unreadOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetPersonNotificationsQuery,
            IReadOnlyList<NotificationMessageResponse>
        >(new GetPersonNotificationsQuery(recipientPersonId, unreadOnly ?? false), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<NotificationMessageResponse>, ProblemHttpResult>
    > MarkReadAsync(Guid id, ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<MarkNotificationReadCommand, NotificationMessageResponse>(
            new MarkNotificationReadCommand(id),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FollowingSubscriptionResponse>>, ProblemHttpResult>
    > ListFollowingsAsync(Guid personId, IQueryDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.QueryAsync<
            GetPersonFollowingsQuery,
            IReadOnlyList<FollowingSubscriptionResponse>
        >(new GetPersonFollowingsQuery(personId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<FollowingSubscriptionResponse>, ProblemHttpResult>
    > ToggleFollowingAsync(
        ToggleFollowingSubscriptionCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<
            ToggleFollowingSubscriptionCommand,
            FollowingSubscriptionResponse
        >(body, ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Created<Guid>, ProblemHttpResult>> RecordFollowedEventAsync(
        RecordFollowedEntityEventCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<RecordFollowedEntityEventCommand, Guid>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/following/events/{r.Value:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<FollowingDigestRunResponse>, ProblemHttpResult>
    > DispatchDigestsAsync(ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<DispatchFollowingDigestsCommand, FollowingDigestRunResponse>(
            new DispatchFollowingDigestsCommand(),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
