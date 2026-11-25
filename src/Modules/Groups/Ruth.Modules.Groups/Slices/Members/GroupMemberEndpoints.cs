using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Members;

internal static class GroupMemberEndpoints
{
    public static IEndpointRouteBuilder MapGroupMemberEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups")
            .WithTags("Group Members")
            .RequireAuthorization();

        MapMemberCrudRoutes(group);
        MapRegistrationAndPlacementRoutes(group);
        return endpoints;
    }

    private static void MapMemberCrudRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/{groupId:guid}/members", GetMembersAsync)
            .WithName("GroupsListMembers")
            .WithSummary("List members belonging to a group")
            .Produces<IReadOnlyList<GroupMemberResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{groupId:guid}/members", AddMemberAsync)
            .WithName("GroupsAddMember")
            .WithSummary("Add a person to a group with capacity, role, and requirement checks")
            .Produces<GroupMemberResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPut("/members/{memberId:guid}", UpdateMemberAsync)
            .WithName("GroupsUpdateMember")
            .WithSummary("Update a group member's role, status, or scheduling preferences")
            .Produces<GroupMemberResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/members/{memberId:guid}", RemoveMemberAsync)
            .WithName("GroupsRemoveMember")
            .WithSummary("Archive or remove a group member")
            .Produces<bool>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/members/{memberId:guid}/history", GetMemberHistoryAsync)
            .WithName("GroupsGetMemberHistory")
            .WithSummary("Get SCD Type-2 historical snapshots for a group member")
            .Produces<IReadOnlyList<GroupMemberHistoricalResponse>>(StatusCodes.Status200OK);
    }

    private static void MapRegistrationAndPlacementRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/people/{personId:guid}/memberships", GetPersonMembershipsAsync)
            .WithName("GroupsListPersonMemberships")
            .WithSummary("List all group memberships for a CRM Person")
            .Produces<IReadOnlyList<GroupMemberResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{groupId:guid}/simple-register", SimpleRegisterAsync)
            .WithName("GroupsSimpleRegister")
            .WithSummary("Self-service GroupSimpleRegister registration for a public active group")
            .Produces<GroupMemberResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPost("/placement/board", GetPlacementBoardAsync)
            .WithName("GroupsGetPlacementBoard")
            .WithSummary("Load source and destination columns for the GroupPlacement board")
            .Produces<GroupPlacementBoardResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/placement/execute", PlaceMembersAsync)
            .WithName("GroupsPlaceMembers")
            .WithSummary("Place one or more people into destination groups")
            .Produces<GroupPlacementResultResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberResponse>>, ProblemHttpResult>
    > GetMembersAsync(
        Guid groupId,
        GroupMemberStatus? status,
        bool? includeArchived,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(groupId, status, includeArchived ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupMemberResponse>, ProblemHttpResult>
    > AddMemberAsync(
        Guid groupId,
        AddGroupMemberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            command with
            {
                GroupId = groupId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/groups/members/{result.Value.Id:D}", result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupMemberResponse>, ProblemHttpResult>
    > UpdateMemberAsync(
        Guid memberId,
        UpdateGroupMemberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateGroupMemberCommand, GroupMemberResponse>(
            command with
            {
                GroupMemberId = memberId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> RemoveMemberAsync(
        Guid memberId,
        bool? archiveInsteadOfDelete,
        Guid? archivedByPersonId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveGroupMemberCommand, bool>(
            new RemoveGroupMemberCommand(
                memberId,
                archiveInsteadOfDelete ?? true,
                archivedByPersonId
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberHistoricalResponse>>, ProblemHttpResult>
    > GetMemberHistoryAsync(
        Guid memberId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupMemberHistoryQuery,
            IReadOnlyList<GroupMemberHistoricalResponse>
        >(new GetGroupMemberHistoryQuery(memberId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberResponse>>, ProblemHttpResult>
    > GetPersonMembershipsAsync(
        Guid personId,
        bool? activeOnly,
        bool? includeArchived,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonGroupMembershipsQuery,
            IReadOnlyList<GroupMemberResponse>
        >(
            new GetPersonGroupMembershipsQuery(
                personId,
                activeOnly ?? true,
                includeArchived ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupMemberResponse>, ProblemHttpResult>
    > SimpleRegisterAsync(
        Guid groupId,
        SimpleRegisterForGroupCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<SimpleRegisterForGroupCommand, GroupMemberResponse>(
            command with
            {
                GroupId = groupId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/groups/members/{result.Value.Id:D}", result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupPlacementBoardResponse>, ProblemHttpResult>
    > GetPlacementBoardAsync(
        GetGroupPlacementBoardQuery query,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupPlacementBoardQuery,
            GroupPlacementBoardResponse
        >(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupPlacementResultResponse>, ProblemHttpResult>
    > PlaceMembersAsync(
        PlaceGroupMembersCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PlaceGroupMembersCommand,
            GroupPlacementResultResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
