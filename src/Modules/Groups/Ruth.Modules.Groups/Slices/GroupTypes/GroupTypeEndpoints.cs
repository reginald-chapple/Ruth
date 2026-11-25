using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal static class GroupTypeEndpoints
{
    public static IEndpointRouteBuilder MapGroupTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups/types")
            .WithTags("Group Types")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("GroupsListGroupTypes")
            .WithSummary("List configured GroupTypes")
            .Produces<IReadOnlyList<GroupTypeSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GroupsGetGroupTypeById")
            .WithSummary("Get GroupType details including roles and allowed child types")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("GroupsCreateGroupType")
            .WithSummary("Create a new GroupType")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("GroupsUpdateGroupType")
            .WithSummary("Update an existing GroupType")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/roles", AddRoleAsync)
            .WithName("GroupsAddGroupTypeRole")
            .WithSummary("Add a role to a GroupType")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/{id:guid}/roles/{roleId:guid}", UpdateRoleAsync)
            .WithName("GroupsUpdateGroupTypeRole")
            .WithSummary("Update a role on a GroupType")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}/roles/{roleId:guid}", RemoveRoleAsync)
            .WithName("GroupsRemoveGroupTypeRole")
            .WithSummary("Remove an unused role from a GroupType")
            .Produces<GroupTypeDetailResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupTypeSummaryResponse>>, ProblemHttpResult>
    > ListAsync(
        bool? navigationOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupTypesQuery,
            IReadOnlyList<GroupTypeSummaryResponse>
        >(new GetGroupTypesQuery(navigationOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupTypeDetailResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetGroupTypeByIdQuery, GroupTypeDetailResponse>(
            new GetGroupTypeByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupTypeDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateGroupTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/groups/types/{result.Value.Id:D}", result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupTypeDetailResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateGroupTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateGroupTypeCommand, GroupTypeDetailResponse>(
            command with
            {
                GroupTypeId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<GroupTypeDetailResponse>, ProblemHttpResult>> AddRoleAsync(
        Guid id,
        AddGroupTypeRoleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddGroupTypeRoleCommand, GroupTypeDetailResponse>(
            command with
            {
                GroupTypeId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupTypeDetailResponse>, ProblemHttpResult>
    > UpdateRoleAsync(
        Guid id,
        Guid roleId,
        GroupTypeRoleInputDto role,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateGroupTypeRoleCommand,
            GroupTypeDetailResponse
        >(new UpdateGroupTypeRoleCommand(id, roleId, role), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupTypeDetailResponse>, ProblemHttpResult>
    > RemoveRoleAsync(
        Guid id,
        Guid roleId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RemoveGroupTypeRoleCommand,
            GroupTypeDetailResponse
        >(new RemoveGroupTypeRoleCommand(id, roleId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
