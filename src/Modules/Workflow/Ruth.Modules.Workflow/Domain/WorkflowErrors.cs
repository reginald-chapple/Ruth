using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Domain errors for the Workflow bounded context across W5-S5 and W5-S6.</summary>
public static class WorkflowErrors
{
    public static readonly DomainError WorkflowTypeInvalid = DomainError.Validation(
        "Workflow.WorkflowTypeInvalid",
        "WorkflowType requires a valid name, slug, work term, prefix, positive intervals, and at least one active initial activity."
    );

    public static readonly DomainError WorkflowTypeNotFound = DomainError.NotFound(
        "Workflow.WorkflowTypeNotFound",
        "WorkflowType was not found."
    );

    public static readonly DomainError WorkflowTypeSlugConflict = DomainError.Conflict(
        "Workflow.WorkflowTypeSlugConflict",
        "A WorkflowType with the specified slug already exists."
    );

    public static readonly DomainError WorkflowTypeInactive = DomainError.Conflict(
        "Workflow.WorkflowTypeInactive",
        "Cannot launch a new instance of an inactive WorkflowType."
    );

    public static readonly DomainError WorkflowNotFound = DomainError.NotFound(
        "Workflow.WorkflowNotFound",
        "Workflow instance was not found."
    );

    public static readonly DomainError WorkflowAlreadyClosed = DomainError.Conflict(
        "Workflow.WorkflowAlreadyClosed",
        "Terminal workflows (Completed, Faulted, or Cancelled) cannot be mutated or reprocessed."
    );

    public static readonly DomainError ActivityNotFound = DomainError.NotFound(
        "Workflow.ActivityNotFound",
        "Target WorkflowActivity or WorkflowActivityType was not found."
    );

    public static readonly DomainError ActionPluginNotFound = DomainError.Validation(
        "Workflow.ActionPluginNotFound",
        "Referenced IWorkflowAction plugin key is not registered."
    );

    public static readonly DomainError StepBudgetExceeded = DomainError.Conflict(
        "Workflow.StepBudgetExceeded",
        "Workflow execution exceeded the maximum per-pass step limit, indicating a control-flow cycle."
    );

    public static readonly DomainError TriggerInvalid = DomainError.Validation(
        "Workflow.TriggerInvalid",
        "WorkflowTrigger requires a non-empty name, event type key, and valid target WorkflowTypeId."
    );

    public static readonly DomainError TriggerNotFound = DomainError.NotFound(
        "Workflow.TriggerNotFound",
        "WorkflowTrigger was not found."
    );

    public static readonly DomainError FormTemplateInvalid = DomainError.Validation(
        "Workflow.FormTemplateInvalid",
        "FormBuilderTemplate requires a valid name, slug, and confirmation title."
    );

    public static readonly DomainError FormTemplateNotFound = DomainError.NotFound(
        "Workflow.FormTemplateNotFound",
        "FormBuilderTemplate was not found."
    );

    public static readonly DomainError FormTemplateSlugConflict = DomainError.Conflict(
        "Workflow.FormTemplateSlugConflict",
        "A FormBuilderTemplate with the specified slug already exists."
    );

    public static readonly DomainError ActionFormInvalid = DomainError.Validation(
        "Workflow.ActionFormInvalid",
        "WorkflowActionForm requires at least one field, at least one unique outcome button, and valid column spans."
    );

    public static readonly DomainError ActionFormNotFound = DomainError.NotFound(
        "Workflow.ActionFormNotFound",
        "Interactive WorkflowActionForm was not found for the specified action."
    );

    public static readonly DomainError FormSubmissionInvalid = DomainError.Validation(
        "Workflow.FormSubmissionInvalid",
        "Interactive form submission failed required-field, type, or button selection validation."
    );
}
