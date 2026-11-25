using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Groups;

internal static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        MapCrudAndTreeRoutes(group);
        MapLocationDemographicAndFinderRoutes(group);
        return endpoints;
    }

    private static void MapCrudAndTreeRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/tree", GetTreeAsync)
            .WithName("GroupsGetTree")
            .WithSummary("Get hierarchical group tree")
            .Produces<IReadOnlyList<GroupTreeNodeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GroupsGetById")
            .WithSummary("Get group detail with locations, demographics, and attributes")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("GroupsCreate")
            .WithSummary("Create a new group")
            .Produces<GroupDetailResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("GroupsUpdate")
            .WithSummary("Update an existing group")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/archive", ArchiveAsync)
            .WithName("GroupsSetArchiveState")
            .WithSummary("Archive or unarchive a group")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/{id:guid}/attributes", UpdateAttributesAsync)
            .WithName("GroupsUpdateAttributes")
            .WithSummary("Update JSONB attributes on a group")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}/history", GetHistoryAsync)
            .WithName("GroupsGetHistory")
            .WithSummary("Get SCD Type-2 historical snapshots for a group")
            .Produces<IReadOnlyList<GroupHistoricalResponse>>(StatusCodes.Status200OK);
    }

    private static void MapLocationDemographicAndFinderRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/locations", AttachLocationAsync)
            .WithName("GroupsAttachLocation")
            .WithSummary("Attach or update a PostGIS GroupLocation on a group")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}/locations/{groupLocationId:guid}", RemoveLocationAsync)
            .WithName("GroupsRemoveLocation")
            .WithSummary("Remove a GroupLocation from a group")
            .Produces<GroupDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/demographic-types", GetDemographicTypesAsync)
            .WithName("GroupsListDemographicTypes")
            .WithSummary("List GroupDemographicTypes")
            .Produces<IReadOnlyList<GroupDemographicTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/demographic-types", CreateDemographicTypeAsync)
            .WithName("GroupsCreateDemographicType")
            .WithSummary("Create a GroupDemographicType")
            .Produces<GroupDemographicTypeResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPut("/{id:guid}/demographics", SetDemographicValueAsync)
            .WithName("GroupsSetDemographicValue")
            .WithSummary("Set a demographic value on a group")
            .Produces<GroupDemographicValueResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/finder", FindGroupsAsync)
            .WithName("GroupsFinderSearch")
            .WithSummary("PostGIS radius, polygon, geofence, and capacity GroupFinder search")
            .Produces<IReadOnlyList<GroupFinderItemResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupTreeNodeResponse>>, ProblemHttpResult>
    > GetTreeAsync(
        Guid? groupTypeId,
        Guid? campusId,
        bool? includeInactive,
        bool? includeArchived,
        bool? navigationOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupTreeQuery,
            IReadOnlyList<GroupTreeNodeResponse>
        >(
            new GetGroupTreeQuery(
                groupTypeId,
                campusId,
                includeInactive ?? false,
                includeArchived ?? false,
                navigationOnly ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupDetailResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetGroupByIdQuery, GroupDetailResponse>(
            new GetGroupByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<GroupDetailResponse>, ProblemHttpResult>> CreateAsync(
        CreateGroupCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/groups/{result.Value.Id:D}", result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupDetailResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateGroupCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateGroupCommand, GroupDetailResponse>(
            command with
            {
                GroupId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupDetailResponse>, ProblemHttpResult>> ArchiveAsync(
        Guid id,
        SetGroupArchiveStateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<SetGroupArchiveStateCommand, GroupDetailResponse>(
            command with
            {
                GroupId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupDetailResponse>, ProblemHttpResult>
    > UpdateAttributesAsync(
        Guid id,
        UpdateGroupAttributesCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateGroupAttributesCommand, GroupDetailResponse>(
            command with
            {
                GroupId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupHistoricalResponse>>, ProblemHttpResult>
    > GetHistoryAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupHistoryQuery,
            IReadOnlyList<GroupHistoricalResponse>
        >(new GetGroupHistoryQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupDetailResponse>, ProblemHttpResult>
    > AttachLocationAsync(
        Guid id,
        AttachGroupLocationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AttachGroupLocationCommand, GroupDetailResponse>(
            command with
            {
                GroupId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupDetailResponse>, ProblemHttpResult>
    > RemoveLocationAsync(
        Guid id,
        Guid groupLocationId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveGroupLocationCommand, GroupDetailResponse>(
            new RemoveGroupLocationCommand(id, groupLocationId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupDemographicTypeResponse>>, ProblemHttpResult>
    > GetDemographicTypesAsync(
        Guid? groupTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupDemographicTypesQuery,
            IReadOnlyList<GroupDemographicTypeResponse>
        >(new GetGroupDemographicTypesQuery(groupTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupDemographicTypeResponse>, ProblemHttpResult>
    > CreateDemographicTypeAsync(
        CreateGroupDemographicTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateGroupDemographicTypeCommand,
            GroupDemographicTypeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/demographic-types/{result.Value.Id:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupDemographicValueResponse>, ProblemHttpResult>
    > SetDemographicValueAsync(
        Guid id,
        SetGroupDemographicValueCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetGroupDemographicValueCommand,
            GroupDemographicValueResponse
        >(command with { GroupId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupFinderItemResponse>>, ProblemHttpResult>
    > FindGroupsAsync(
        FindGroupsQuery query,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>
        >(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
