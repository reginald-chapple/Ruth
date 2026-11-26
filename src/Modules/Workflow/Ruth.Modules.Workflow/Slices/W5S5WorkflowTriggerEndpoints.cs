using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices;

internal static class W5S5WorkflowTriggerEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowTriggerEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/workflows/triggers")
            .WithTags("Workflows")
            .RequireAuthorization();

        MapTriggerCrudRoutes(group);
        MapTriggerDispatchRoutes(group);
        return endpoints;
    }

    private static void MapTriggerCrudRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "",
                static async (
                    bool? includeInactive,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowTriggersQuery,
                            IReadOnlyList<WorkflowTriggerResponse>
                        >(new GetWorkflowTriggersQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetWorkflowTriggers")
            .WithSummary("Lists automated WorkflowTrigger rules");

        _ = group
            .MapPost(
                "",
                static async (
                    WorkflowTriggerSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        CreateWorkflowTriggerCommand,
                        WorkflowTriggerResponse
                    >(new CreateWorkflowTriggerCommand(spec), ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/workflows/triggers/{res.Value.Id:D}", res.Value)
                        : WorkflowHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateWorkflowTrigger")
            .WithSummary("Creates an automated WorkflowTrigger rule");

        _ = group
            .MapPut(
                "/{id:guid}",
                static async (
                    Guid id,
                    WorkflowTriggerSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            UpdateWorkflowTriggerCommand,
                            WorkflowTriggerResponse
                        >(new UpdateWorkflowTriggerCommand(id, spec), ct)
                    )
            )
            .WithName("UpdateWorkflowTrigger")
            .WithSummary("Updates an automated WorkflowTrigger rule");
    }

    private static void MapTriggerDispatchRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/dispatch",
                static async (
                    DispatchWorkflowAutomationEventCommand cmd,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            DispatchWorkflowAutomationEventCommand,
                            WorkflowAutomationDispatchResponse
                        >(cmd, ct)
                    )
            )
            .WithName("DispatchWorkflowAutomationEvent")
            .WithSummary("Evaluates active WorkflowTriggers against an automation stimulus");
    }
}
