using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal static class GroupRequirementEndpoints
{
    public static IEndpointRouteBuilder MapGroupRequirementEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups/requirements")
            .WithTags("Group Requirements")
            .RequireAuthorization();

        _ = group
            .MapGet("/types", GetTypesAsync)
            .WithName("GroupsListRequirementTypes")
            .WithSummary("List reusable GroupRequirementTypes")
            .Produces<IReadOnlyList<GroupRequirementTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/types", CreateTypeAsync)
            .WithName("GroupsCreateRequirementType")
            .WithSummary("Create a reusable GroupRequirementType")
            .Produces<GroupRequirementTypeResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/", GetRequirementsAsync)
            .WithName("GroupsListRequirements")
            .WithSummary("List GroupRequirements for a Group or GroupType")
            .Produces<IReadOnlyList<GroupRequirementResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", AddRequirementAsync)
            .WithName("GroupsAddRequirement")
            .WithSummary("Attach a GroupRequirement to a Group or GroupType")
            .Produces<GroupRequirementResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/members/{memberId:guid}", GetMemberRequirementsAsync)
            .WithName("GroupsGetMemberRequirements")
            .WithSummary("Get persisted requirement evaluations for a GroupMember")
            .Produces<IReadOnlyList<GroupMemberRequirementResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/members/{memberId:guid}/evaluate", EvaluateMemberAsync)
            .WithName("GroupsEvaluateMemberRequirements")
            .WithSummary("Evaluate and persist all applicable requirements for a GroupMember")
            .Produces<IReadOnlyList<GroupMemberRequirementResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/members/{memberId:guid}/manual", SetManualStateAsync)
            .WithName("GroupsSetMemberRequirementManualState")
            .WithSummary("Mark a manual requirement complete or apply a leader override")
            .Produces<GroupMemberRequirementResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupRequirementTypeResponse>>, ProblemHttpResult>
    > GetTypesAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupRequirementTypesQuery,
            IReadOnlyList<GroupRequirementTypeResponse>
        >(new GetGroupRequirementTypesQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupRequirementTypeResponse>, ProblemHttpResult>
    > CreateTypeAsync(
        CreateGroupRequirementTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/groups/requirements/types/{result.Value.Id:D}",
                result.Value
            )
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupRequirementResponse>>, ProblemHttpResult>
    > GetRequirementsAsync(
        Guid? groupId,
        Guid? groupTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupRequirementsQuery,
            IReadOnlyList<GroupRequirementResponse>
        >(new GetGroupRequirementsQuery(groupId, groupTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<GroupRequirementResponse>, ProblemHttpResult>
    > AddRequirementAsync(
        AddGroupRequirementCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            AddGroupRequirementCommand,
            GroupRequirementResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/groups/requirements/{result.Value.Id:D}", result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberRequirementResponse>>, ProblemHttpResult>
    > GetMemberRequirementsAsync(
        Guid memberId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupMemberRequirementsQuery,
            IReadOnlyList<GroupMemberRequirementResponse>
        >(new GetGroupMemberRequirementsQuery(memberId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupMemberRequirementResponse>>, ProblemHttpResult>
    > EvaluateMemberAsync(
        Guid memberId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            EvaluateGroupMemberRequirementsCommand,
            IReadOnlyList<GroupMemberRequirementResponse>
        >(new EvaluateGroupMemberRequirementsCommand(memberId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupMemberRequirementResponse>, ProblemHttpResult>
    > SetManualStateAsync(
        Guid memberId,
        SetGroupMemberRequirementManualStateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetGroupMemberRequirementManualStateCommand,
            GroupMemberRequirementResponse
        >(command with { GroupMemberId = memberId }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
