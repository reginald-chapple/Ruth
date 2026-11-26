using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Slices.Forms;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Workflow.Slices;

internal static class W5S6WorkflowFormRegistration
{
    public static void Register(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFormBuilderTemplateCommand,
            FormBuilderTemplateResponse,
            CreateFormBuilderTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFormBuilderTemplateCommand,
            FormBuilderTemplateResponse,
            UpdateFormBuilderTemplateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFormBuilderTemplatesQuery,
            IReadOnlyList<FormBuilderTemplateResponse>,
            GetFormBuilderTemplatesQueryHandler
        >();

        _ = services.AddCommandHandler<
            UpsertWorkflowActionFormCommand,
            WorkflowActionFormResponse,
            UpsertWorkflowActionFormCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowActionFormByActionTypeQuery,
            WorkflowActionFormResponse,
            GetWorkflowActionFormByActionTypeQueryHandler
        >();
        _ = services.AddQueryHandler<
            PreviewFormVisibilityQuery,
            FormVisibilityPreviewResponse,
            PreviewFormVisibilityQueryHandler
        >();

        _ = services.AddQueryHandler<
            GetWorkflowEntryFormQuery,
            WorkflowEntryFormViewResponse,
            GetWorkflowEntryFormQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitWorkflowEntryFormCommand,
            WorkflowFormSubmissionResponse,
            SubmitWorkflowEntryFormCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetMyWorkflowTasksQuery,
            IReadOnlyList<WorkflowTaskItemResponse>,
            GetMyWorkflowTasksQueryHandler
        >();
        _ = services.AddCommandHandler<
            LaunchFormBuilderSubmissionCommand,
            WorkflowFormSubmissionResponse,
            LaunchFormBuilderSubmissionCommandHandler
        >();
    }
}
