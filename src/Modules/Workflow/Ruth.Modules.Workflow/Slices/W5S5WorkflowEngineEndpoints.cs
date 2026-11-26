using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices;

internal static class W5S5WorkflowEngineEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEngineEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/workflows")
            .WithTags("Workflows")
            .RequireAuthorization();
        MapDefinitionReadRoutes(group);
        MapDefinitionWriteRoutes(group);
        MapInstanceQueryRoutes(group);
        MapInstanceLaunchAndProcessRoutes(group);
        MapInstanceMutationRoutes(group);
        return endpoints;
    }

    private static void MapDefinitionReadRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/actions/catalog",
                static async (IQueryDispatcher queries, CancellationToken ct) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowActionCatalogQuery,
                            IReadOnlyList<WorkflowActionPluginDescriptor>
                        >(new GetWorkflowActionCatalogQuery(), ct)
                    )
            )
            .WithName("GetWorkflowActionCatalog")
            .WithSummary("Lists all registered modular IWorkflowAction plugins");

        _ = group
            .MapGet(
                "/types",
                static async (
                    bool? includeInactive,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowTypesQuery,
                            IReadOnlyList<WorkflowTypeResponse>
                        >(new GetWorkflowTypesQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetWorkflowTypes")
            .WithSummary("Lists WorkflowType definitions");

        _ = group
            .MapGet(
                "/types/{idOrSlug}",
                static async (string idOrSlug, IQueryDispatcher queries, CancellationToken ct) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowTypeByIdOrSlugQuery,
                            WorkflowTypeResponse
                        >(new GetWorkflowTypeByIdOrSlugQuery(idOrSlug), ct)
                    )
            )
            .WithName("GetWorkflowTypeByIdOrSlug")
            .WithSummary("Gets a WorkflowType definition by UUID or slug");
    }

    private static void MapDefinitionWriteRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/types",
                static async (
                    WorkflowTypeSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        CreateWorkflowTypeCommand,
                        WorkflowTypeResponse
                    >(new CreateWorkflowTypeCommand(spec), ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/workflows/types/{res.Value.Id:D}", res.Value)
                        : WorkflowHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateWorkflowType")
            .WithSummary("Creates a new WorkflowType state-machine definition");

        _ = group
            .MapPut(
                "/types/{id:guid}",
                static async (
                    Guid id,
                    WorkflowTypeSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<UpdateWorkflowTypeCommand, WorkflowTypeResponse>(
                            new UpdateWorkflowTypeCommand(id, spec),
                            ct
                        )
                    )
            )
            .WithName("UpdateWorkflowType")
            .WithSummary("Updates an existing WorkflowType definition");
    }

    private static void MapInstanceQueryRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/instances",
                static async (
                    Guid? workflowTypeId,
                    WorkflowInstanceStatus? status,
                    Guid? initiatorPersonId,
                    int? limit,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowInstancesQuery,
                            IReadOnlyList<WorkflowInstanceResponse>
                        >(
                            new GetWorkflowInstancesQuery(
                                workflowTypeId,
                                status,
                                initiatorPersonId,
                                limit ?? 50
                            ),
                            ct
                        )
                    )
            )
            .WithName("GetWorkflowInstances")
            .WithSummary("Lists Workflow instances");

        _ = group
            .MapGet(
                "/instances/{id:guid}",
                static async (Guid id, IQueryDispatcher queries, CancellationToken ct) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowInstanceByIdQuery,
                            WorkflowInstanceResponse
                        >(new GetWorkflowInstanceByIdQuery(id), ct)
                    )
            )
            .WithName("GetWorkflowInstanceById")
            .WithSummary("Gets a Workflow instance with activities, actions, and logs");
    }

    private static void MapInstanceLaunchAndProcessRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/instances/launch",
                static async (
                    LaunchWorkflowCommand cmd,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        LaunchWorkflowCommand,
                        WorkflowInstanceResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/workflows/instances/{res.Value.Id:D}", res.Value)
                        : WorkflowHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("LaunchWorkflowInstance")
            .WithSummary("Launches a new Workflow instance");

        _ = group
            .MapPost(
                "/instances/{id:guid}/process",
                static async (Guid id, ICommandDispatcher commands, CancellationToken ct) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<ProcessWorkflowCommand, WorkflowInstanceResponse>(
                            new ProcessWorkflowCommand(id),
                            ct
                        )
                    )
            )
            .WithName("ProcessWorkflowInstance")
            .WithSummary("Runs one processing pass on a Workflow instance");

        _ = group
            .MapPost(
                "/instances/process-pending",
                static async (
                    ProcessPendingWorkflowsCommand cmd,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            ProcessPendingWorkflowsCommand,
                            WorkflowBatchProcessResponse
                        >(cmd, ct)
                    )
            )
            .WithName("ProcessPendingWorkflows")
            .WithSummary("Processes due Pending, Active, and Suspended workflows");
    }

    private static void MapInstanceMutationRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/instances/{id:guid}/cancel",
                static async (
                    Guid id,
                    CancelWorkflowCommand body,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<CancelWorkflowCommand, WorkflowInstanceResponse>(
                            body with
                            {
                                WorkflowId = id,
                            },
                            ct
                        )
                    )
            )
            .WithName("CancelWorkflowInstance")
            .WithSummary("Cancels an open Workflow instance");

        _ = group
            .MapPost(
                "/instances/{id:guid}/assign",
                static async (
                    Guid id,
                    AssignWorkflowActivityCommand body,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            AssignWorkflowActivityCommand,
                            WorkflowInstanceResponse
                        >(body with { WorkflowId = id }, ct)
                    )
            )
            .WithName("AssignWorkflowActivity")
            .WithSummary("Assigns an open WorkflowActivity to a person or group");
    }
}
