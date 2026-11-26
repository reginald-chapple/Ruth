using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices;

internal static class W5S6WorkflowFormEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowFormEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var staffGroup = endpoints
            .MapGroup("/api/workflows/forms")
            .WithTags("Workflow Forms")
            .RequireAuthorization();

        MapTemplateReadAndCreateRoutes(staffGroup);
        MapTemplateUpdateAndActionFormRoutes(staffGroup);
        MapInboxRoutes(staffGroup);
        MapEntrySubmissionRoutes(staffGroup);
        MapPublicFormRoutes(endpoints);
        return endpoints;
    }

    private static void MapTemplateReadAndCreateRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/templates",
                static async (
                    bool? includeInactive,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetFormBuilderTemplatesQuery,
                            IReadOnlyList<FormBuilderTemplateResponse>
                        >(new GetFormBuilderTemplatesQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetFormBuilderTemplates")
            .WithSummary("Lists reusable FormBuilderTemplates");

        _ = group
            .MapPost(
                "/templates",
                static async (
                    FormBuilderTemplateSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        CreateFormBuilderTemplateCommand,
                        FormBuilderTemplateResponse
                    >(new CreateFormBuilderTemplateCommand(spec), ct);
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/workflows/forms/templates/{res.Value.Id:D}",
                            res.Value
                        )
                        : WorkflowHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateFormBuilderTemplate")
            .WithSummary("Creates a reusable FormBuilderTemplate");
    }

    private static void MapTemplateUpdateAndActionFormRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPut(
                "/templates/{id:guid}",
                static async (
                    Guid id,
                    FormBuilderTemplateSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            UpdateFormBuilderTemplateCommand,
                            FormBuilderTemplateResponse
                        >(new UpdateFormBuilderTemplateCommand(id, spec), ct)
                    )
            )
            .WithName("UpdateFormBuilderTemplate")
            .WithSummary("Updates a FormBuilderTemplate");

        _ = group
            .MapGet(
                "/actions/{actionTypeId:guid}",
                static async (Guid actionTypeId, IQueryDispatcher queries, CancellationToken ct) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowActionFormByActionTypeQuery,
                            WorkflowActionFormResponse
                        >(new GetWorkflowActionFormByActionTypeQuery(actionTypeId), ct)
                    )
            )
            .WithName("GetWorkflowActionForm")
            .WithSummary("Gets the interactive WorkflowActionForm bound to an action definition");

        _ = group
            .MapPut(
                "/actions/{actionTypeId:guid}",
                static async (
                    Guid actionTypeId,
                    WorkflowActionFormSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            UpsertWorkflowActionFormCommand,
                            WorkflowActionFormResponse
                        >(new UpsertWorkflowActionFormCommand(actionTypeId, spec), ct)
                    )
            )
            .WithName("UpsertWorkflowActionForm")
            .WithSummary("Creates or updates an interactive WorkflowActionForm");
    }

    private static void MapInboxRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/preview-visibility",
                static async (
                    PreviewFormVisibilityQuery query,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            PreviewFormVisibilityQuery,
                            FormVisibilityPreviewResponse
                        >(query, ct)
                    )
            )
            .WithName("PreviewWorkflowFormVisibility")
            .WithSummary("Evaluates conditional section and field visibility rules");

        _ = group
            .MapGet(
                "/tasks",
                static async (
                    Guid? assignedPersonId,
                    Guid? assignedGroupId,
                    bool? includeUnassigned,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetMyWorkflowTasksQuery,
                            IReadOnlyList<WorkflowTaskItemResponse>
                        >(
                            new GetMyWorkflowTasksQuery(
                                assignedPersonId,
                                assignedGroupId,
                                includeUnassigned ?? true
                            ),
                            ct
                        )
                    )
            )
            .WithName("GetMyWorkflowTasks")
            .WithSummary("Lists open interactive WorkflowActivity tasks for MyTasks inbox");
    }

    private static void MapEntrySubmissionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/entry/{workflowId:guid}",
                static async (
                    Guid workflowId,
                    Guid? activityId,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetWorkflowEntryFormQuery,
                            WorkflowEntryFormViewResponse
                        >(new GetWorkflowEntryFormQuery(workflowId, activityId), ct)
                    )
            )
            .WithName("GetWorkflowEntryForm")
            .WithSummary("Loads and evaluates the active interactive WorkflowEntry form");

        _ = group
            .MapPost(
                "/entry/{workflowId:guid}/submit",
                static async (
                    Guid workflowId,
                    SubmitWorkflowEntryFormCommand body,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    WorkflowHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            SubmitWorkflowEntryFormCommand,
                            WorkflowFormSubmissionResponse
                        >(body with { WorkflowId = workflowId }, ct)
                    )
            )
            .WithName("SubmitWorkflowEntryForm")
            .WithSummary("Submits an interactive WorkflowEntry form and resumes processing");
    }

    private static void MapPublicFormRoutes(IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints
            .MapGroup("/api/workflows/public/forms")
            .WithTags("Workflow Public Forms")
            .AllowAnonymous();

        _ = publicGroup
            .MapPost(
                "/submit",
                static async (
                    LaunchFormBuilderSubmissionCommand cmd,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        LaunchFormBuilderSubmissionCommand,
                        WorkflowFormSubmissionResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/workflows/instances/{res.Value.Workflow.Id:D}",
                            res.Value
                        )
                        : WorkflowHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("SubmitPublicFormBuilderWorkflow")
            .WithSummary("Launches and submits an interactive FormBuilderTemplate workflow");
    }
}
