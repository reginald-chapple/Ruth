using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Sync;

internal static class GroupSyncEndpoints
{
    public static IEndpointRouteBuilder MapGroupSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups/sync")
            .WithTags("Group Sync")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListRulesAsync)
            .WithName("GroupsListSyncRules")
            .WithSummary("List GroupSync rules")
            .Produces<IReadOnlyList<GroupSyncRuleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/", UpsertRuleAsync)
            .WithName("GroupsUpsertSyncRule")
            .WithSummary("Create or update a GroupSync rule")
            .Produces<GroupSyncRuleResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{ruleId:guid}", DeleteRuleAsync)
            .WithName("GroupsDeleteSyncRule")
            .WithSummary("Delete a GroupSync rule")
            .Produces<bool>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/run", RunSyncAsync)
            .WithName("GroupsRunSync")
            .WithSummary("Run GroupSync reconciliation immediately")
            .Produces<IReadOnlyList<GroupSyncExecutionResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupSyncRuleResponse>>, ProblemHttpResult>
    > ListRulesAsync(
        Guid? groupId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupSyncRulesQuery,
            IReadOnlyList<GroupSyncRuleResponse>
        >(new GetGroupSyncRulesQuery(groupId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GroupSyncRuleResponse>, ProblemHttpResult>
    > UpsertRuleAsync(
        UpsertGroupSyncRuleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpsertGroupSyncRuleCommand, GroupSyncRuleResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<bool>, ProblemHttpResult>> DeleteRuleAsync(
        Guid ruleId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteGroupSyncRuleCommand, bool>(
            new DeleteGroupSyncRuleCommand(ruleId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GroupSyncExecutionResponse>>, ProblemHttpResult>
    > RunSyncAsync(
        RunGroupSyncCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RunGroupSyncCommand,
            IReadOnlyList<GroupSyncExecutionResponse>
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
