using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal static class SchedulingEndpoints
{
    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups/scheduling")
            .WithTags("Serving Scheduler & RSVPs")
            .RequireAuthorization();

        MapConfigAndExclusionRoutes(group);
        MapGridToolboxAndRsvpRoutes(group);
        return endpoints;
    }

    private static void MapConfigAndExclusionRoutes(RouteGroupBuilder group)
    {
        MapConfigAndAssignmentRoutes(group);
        MapExclusionRoutes(group);
    }

    private static void MapConfigAndAssignmentRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/templates", GetTemplatesAsync)
            .WithName("GroupsListScheduleTemplates")
            .WithSummary("List GroupMemberScheduleTemplates")
            .Produces<IReadOnlyList<GroupMemberScheduleTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/templates", CreateTemplateAsync)
            .WithName("GroupsCreateScheduleTemplate")
            .WithSummary("Create a GroupMemberScheduleTemplate")
            .Produces<GroupMemberScheduleTemplateResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/{groupId:guid}/location-configs", GetLocationConfigsAsync)
            .WithName("GroupsListLocationScheduleConfigs")
            .WithSummary("List GroupLocationScheduleConfig thresholds for a Group")
            .Produces<IReadOnlyList<GroupLocationScheduleConfigResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/location-configs", UpsertLocationConfigAsync)
            .WithName("GroupsUpsertLocationScheduleConfig")
            .WithSummary(
                "Upsert min/desired/max volunteer thresholds for a GroupLocation + Schedule"
            )
            .Produces<GroupLocationScheduleConfigResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{groupId:guid}/assignments", GetAssignmentsAsync)
            .WithName("GroupsListMemberAssignments")
            .WithSummary("List standing GroupMemberAssignments for a Group")
            .Produces<IReadOnlyList<GroupMemberAssignmentResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/assignments", UpsertAssignmentAsync)
            .WithName("GroupsUpsertMemberAssignment")
            .WithSummary("Add or retrieve a standing GroupMemberAssignment")
            .Produces<GroupMemberAssignmentResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/assignments/{assignmentId:guid}", RemoveAssignmentAsync)
            .WithName("GroupsRemoveMemberAssignment")
            .WithSummary("Remove a standing GroupMemberAssignment")
            .Produces<bool>(StatusCodes.Status200OK);
    }

    private static void MapExclusionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/exclusions/person", AddPersonExclusionAsync)
            .WithName("GroupsAddPersonExclusion")
            .WithSummary("Add a personal blackout date range")
            .Produces<PersonScheduleExclusionResponse>(StatusCodes.Status201Created);

        _ = group
            .MapDelete("/exclusions/person/{exclusionId:guid}", RemovePersonExclusionAsync)
            .WithName("GroupsRemovePersonExclusion")
            .WithSummary("Remove a personal blackout date range")
            .Produces<bool>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/exclusions/group-types/{groupTypeId:guid}", GetGroupExclusionsAsync)
            .WithName("GroupsListGroupTypeExclusions")
            .WithSummary("List GroupType-level schedule exclusions")
            .Produces<IReadOnlyList<GroupScheduleExclusionResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/exclusions/group-types", AddGroupExclusionAsync)
            .WithName("GroupsAddGroupTypeExclusion")
            .WithSummary("Add a GroupType-level schedule exclusion")
            .Produces<GroupScheduleExclusionResponse>(StatusCodes.Status201Created);

        _ = group
            .MapDelete("/exclusions/group-types/{exclusionId:guid}", RemoveGroupExclusionAsync)
            .WithName("GroupsRemoveGroupTypeExclusion")
            .WithSummary("Remove a GroupType-level schedule exclusion")
            .Produces<bool>(StatusCodes.Status200OK);
    }

    private static void MapGridToolboxAndRsvpRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/{groupId:guid}/grid", GetGridAsync)
            .WithName("GroupsGetServingScheduleGrid")
            .WithSummary("Load the Coordinator Schedule Grid for a Group over a date range")
            .Produces<ServingScheduleGridResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/schedule-volunteer", ScheduleVolunteerAsync)
            .WithName("GroupsScheduleVolunteer")
            .WithSummary("Schedule a volunteer for a specific occurrence slot")
            .Produces<ScheduledVolunteerResponse>(StatusCodes.Status201Created);

        _ = group
            .MapDelete("/scheduled/{attendanceId:guid}", UnscheduleVolunteerAsync)
            .WithName("GroupsUnscheduleVolunteer")
            .WithSummary("Remove a scheduled volunteer entry")
            .Produces<bool>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/auto-populate", AutoPopulateAsync)
            .WithName("GroupsAutoPopulateServingSchedule")
            .WithSummary("Auto-populate open serving slots up to DesiredCapacity")
            .Produces<AutoPopulateScheduleResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/toolbox/{personId:guid}", GetToolboxAsync)
            .WithName("GroupsGetVolunteerToolbox")
            .WithSummary("Load upcoming assignments and blackout dates for a volunteer")
            .Produces<VolunteerScheduleToolboxResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/rsvp/{token}", GetRsvpByTokenAsync)
            .AllowAnonymous()
            .WithName("GroupsGetRsvpByToken")
            .WithSummary("Look up a volunteer RSVP link by token")
            .Produces<ScheduledVolunteerResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/rsvp/respond", RespondToRsvpAsync)
            .AllowAnonymous()
            .WithName("GroupsRespondToRsvp")
            .WithSummary("Record a volunteer Accept, Decline, or Maybe RSVP response")
            .Produces<ScheduledVolunteerResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberScheduleTemplateResponse>>, ProblemHttpResult>
    > GetTemplatesAsync(
        Guid? groupTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetScheduleTemplatesQuery,
            IReadOnlyList<GroupMemberScheduleTemplateResponse>
        >(new GetScheduleTemplatesQuery(groupTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupMemberScheduleTemplateResponse>, ProblemHttpResult>
    > CreateTemplateAsync(
        CreateScheduleTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateScheduleTemplateCommand,
            GroupMemberScheduleTemplateResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/scheduling/templates/{result.Value.Id:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupLocationScheduleConfigResponse>>, ProblemHttpResult>
    > GetLocationConfigsAsync(
        Guid groupId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetLocationScheduleConfigsQuery,
            IReadOnlyList<GroupLocationScheduleConfigResponse>
        >(new GetLocationScheduleConfigsQuery(groupId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupLocationScheduleConfigResponse>, ProblemHttpResult>
    > UpsertLocationConfigAsync(
        UpsertLocationScheduleConfigCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpsertLocationScheduleConfigCommand,
            GroupLocationScheduleConfigResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberAssignmentResponse>>, ProblemHttpResult>
    > GetAssignmentsAsync(
        Guid groupId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupMemberAssignmentsQuery,
            IReadOnlyList<GroupMemberAssignmentResponse>
        >(new GetGroupMemberAssignmentsQuery(groupId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupMemberAssignmentResponse>, ProblemHttpResult>
    > UpsertAssignmentAsync(
        UpsertGroupMemberAssignmentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpsertGroupMemberAssignmentCommand,
            GroupMemberAssignmentResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> RemoveAssignmentAsync(
        Guid assignmentId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveGroupMemberAssignmentCommand, bool>(
            new RemoveGroupMemberAssignmentCommand(assignmentId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<PersonScheduleExclusionResponse>, ProblemHttpResult>
    > AddPersonExclusionAsync(
        AddPersonScheduleExclusionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            AddPersonScheduleExclusionCommand,
            PersonScheduleExclusionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/scheduling/exclusions/person/{result.Value.Id:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> RemovePersonExclusionAsync(
        Guid exclusionId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemovePersonScheduleExclusionCommand, bool>(
            new RemovePersonScheduleExclusionCommand(exclusionId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupScheduleExclusionResponse>>, ProblemHttpResult>
    > GetGroupExclusionsAsync(
        Guid groupTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupScheduleExclusionsQuery,
            IReadOnlyList<GroupScheduleExclusionResponse>
        >(new GetGroupScheduleExclusionsQuery(groupTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupScheduleExclusionResponse>, ProblemHttpResult>
    > AddGroupExclusionAsync(
        AddGroupScheduleExclusionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            AddGroupScheduleExclusionCommand,
            GroupScheduleExclusionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/scheduling/exclusions/group-types/{result.Value.Id:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> RemoveGroupExclusionAsync(
        Guid exclusionId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveGroupScheduleExclusionCommand, bool>(
            new RemoveGroupScheduleExclusionCommand(exclusionId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<ServingScheduleGridResponse>, ProblemHttpResult>
    > GetGridAsync(
        Guid groupId,
        DateOnly startDate,
        DateOnly endDate,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetServingScheduleGridQuery,
            ServingScheduleGridResponse
        >(new GetServingScheduleGridQuery(groupId, startDate, endDate), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<ScheduledVolunteerResponse>, ProblemHttpResult>
    > ScheduleVolunteerAsync(
        ScheduleVolunteerCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/scheduling/scheduled/{result.Value.AttendanceId:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> UnscheduleVolunteerAsync(
        Guid attendanceId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UnscheduleVolunteerCommand, bool>(
            new UnscheduleVolunteerCommand(attendanceId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AutoPopulateScheduleResponse>, ProblemHttpResult>
    > AutoPopulateAsync(
        AutoPopulateServingScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            AutoPopulateServingScheduleCommand,
            AutoPopulateScheduleResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<VolunteerScheduleToolboxResponse>, ProblemHttpResult>
    > GetToolboxAsync(
        Guid personId,
        DateOnly? fromDate,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetVolunteerScheduleToolboxQuery,
            VolunteerScheduleToolboxResponse
        >(new GetVolunteerScheduleToolboxQuery(personId, fromDate), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<ScheduledVolunteerResponse>, ProblemHttpResult>
    > GetRsvpByTokenAsync(
        string token,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetRsvpByTokenQuery, ScheduledVolunteerResponse>(
            new GetRsvpByTokenQuery(token),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<ScheduledVolunteerResponse>, ProblemHttpResult>
    > RespondToRsvpAsync(
        RespondToRsvpCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RespondToRsvpCommand, ScheduledVolunteerResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
