using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Domain.Triggers;

namespace Ruth.Modules.Workflow.Slices;

internal static class WorkflowProjectionMapper
{
    public static WorkflowTypeResponse MapWorkflowType(
        WorkflowType entity,
        IReadOnlySet<WorkflowActionTypeId>? actionIdsWithForms = null
    )
    {
        List<WorkflowActivityTypeResponse> activities =
        [
            .. entity
                .Activities.OrderBy(static a => a.Order)
                .Select(a => new WorkflowActivityTypeResponse(
                    a.Id.Value,
                    a.Key,
                    a.Name,
                    a.Order,
                    a.IsActivatedWithWorkflow,
                    a.IsActive,
                    a.Description,
                    a.DefaultAssignedPersonId,
                    a.DefaultAssignedGroupId,
                    [
                        .. a
                            .Actions.OrderBy(static act => act.Order)
                            .Select(act => new WorkflowActionTypeResponse(
                                act.Id.Value,
                                act.Key,
                                act.Name,
                                act.ActionPluginKey,
                                act.Order,
                                act.IsActivityCompletedOnSuccess,
                                act.IsWorkflowCompletedOnSuccess,
                                actionIdsWithForms?.Contains(act.Id) == true,
                                new Dictionary<string, string?>(
                                    act.Configuration,
                                    StringComparer.OrdinalIgnoreCase
                                )
                            )),
                    ]
                )),
        ];

        return new WorkflowTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.WorkTerm,
            entity.WorkflowIdPrefix,
            entity.Description,
            entity.CategoryId,
            entity.IsActive,
            entity.IsPersisted,
            entity.LoggingLevel,
            entity.ProcessingIntervalSeconds,
            entity.MaxProcessingAttempts,
            entity.DeclaredAttributes,
            activities,
            entity.CreatedOnUtc
        );
    }

    public static WorkflowInstanceResponse MapInstance(
        WorkflowInstance entity,
        string workflowTypeName
    )
    {
        return new WorkflowInstanceResponse(
            entity.Id.Value,
            entity.WorkflowTypeId.Value,
            workflowTypeName,
            entity.WorkflowNumber,
            entity.Name,
            entity.Status,
            entity.StateLabel,
            entity.InitiatorPersonId,
            entity.PrimaryEntityTypeKey,
            entity.PrimaryEntityId,
            new Dictionary<string, string?>(entity.Attributes, StringComparer.OrdinalIgnoreCase),
            entity.ActivatedAtUtc,
            entity.LastProcessedAtUtc,
            entity.NextScheduledProcessAtUtc,
            entity.CompletedAtUtc,
            entity.ErrorMessage,
            entity.ProcessingAttemptCount,
            MapActivityInstances(entity.Activities),
            MapLogs(entity.Logs)
        );
    }

    public static WorkflowTriggerResponse MapTrigger(
        WorkflowTrigger trigger,
        string workflowTypeName
    )
    {
        return new WorkflowTriggerResponse(
            trigger.Id.Value,
            trigger.Name,
            trigger.TriggerKind,
            trigger.EventTypeKey,
            trigger.WorkflowTypeId.Value,
            workflowTypeName,
            trigger.EntityTypeQualifier,
            trigger.FilterAttributeKey,
            trigger.FilterOperator,
            trigger.FilterExpectedValue,
            trigger.WorkflowNameTemplate,
            trigger.IsActive,
            trigger.Order
        );
    }

    public static FormBuilderTemplateResponse MapFormTemplate(FormBuilderTemplate template)
    {
        return new FormBuilderTemplateResponse(
            template.Id.Value,
            template.Name,
            template.Slug,
            template.Description,
            template.IsActive,
            template.FormHeaderHtml,
            template.FormFooterHtml,
            template.ConfirmationTitle,
            template.ConfirmationMessageTemplate,
            template.IncludePersonEntrySection,
            template.AllowNotes,
            template.CompletionRedirectUrl,
            template.LinkedWorkflowTypeId,
            template.CreatedOnUtc
        );
    }

    public static WorkflowActionFormResponse MapActionForm(WorkflowActionForm form)
    {
        return new WorkflowActionFormResponse(
            form.Id.Value,
            form.WorkflowActionTypeId.Value,
            form.FormBuilderTemplateId?.Value,
            form.HeaderMarkup,
            form.FooterMarkup,
            form.AllowPersonEntry,
            form.NotificationSystemCommunicationKey,
            [
                .. form
                    .Sections.OrderBy(static s => s.Order)
                    .Select(static s => new WorkflowActionFormSectionResponse(
                        s.Id.Value,
                        s.SectionKey,
                        s.Title,
                        s.Description,
                        s.Order,
                        s.ShowHeadingSeparator,
                        s.VisibilityRule
                    )),
            ],
            [
                .. form
                    .Fields.OrderBy(static f => f.Order)
                    .Select(static f => new WorkflowActionFormFieldResponse(
                        f.Id.Value,
                        f.SectionKey,
                        f.AttributeKey,
                        f.Label,
                        f.FieldTypeKey,
                        f.Order,
                        f.IsVisible,
                        f.IsReadOnly,
                        f.IsRequired,
                        f.DefaultValue,
                        f.HelpText,
                        f.ColumnSpan,
                        f.SelectOptions,
                        f.VisibilityRule
                    )),
            ],
            form.Buttons
        );
    }

    private static List<WorkflowActivityInstanceResponse> MapActivityInstances(
        IReadOnlyList<WorkflowActivity> activities
    )
    {
        return
        [
            .. activities
                .OrderBy(static a => a.Order)
                .ThenBy(static a => a.ActivatedAtUtc)
                .Select(static a => new WorkflowActivityInstanceResponse(
                    a.Id.Value,
                    a.WorkflowActivityTypeId.Value,
                    a.Key,
                    a.Name,
                    a.Order,
                    a.AssignedPersonId,
                    a.AssignedGroupId,
                    a.IsCompleted,
                    a.ActivatedAtUtc,
                    a.CompletedAtUtc,
                    new Dictionary<string, string?>(a.Attributes, StringComparer.OrdinalIgnoreCase),
                    [
                        .. a
                            .Actions.OrderBy(static act => act.Order)
                            .Select(static act => new WorkflowActionInstanceResponse(
                                act.Id.Value,
                                act.WorkflowActionTypeId.Value,
                                act.Key,
                                act.Name,
                                act.ActionPluginKey,
                                act.Order,
                                act.FormActionSelected,
                                act.IsCompleted,
                                act.LastProcessedAtUtc,
                                act.CompletedAtUtc
                            )),
                    ]
                )),
        ];
    }

    private static List<WorkflowLogResponse> MapLogs(IReadOnlyList<WorkflowLog> logs)
    {
        return
        [
            .. logs.OrderBy(static l => l.LoggedAtUtc)
                .Select(static l => new WorkflowLogResponse(
                    l.Id.Value,
                    l.EntryKind,
                    l.Summary,
                    l.ActorPersonId,
                    l.LoggedAtUtc
                )),
        ];
    }
}
