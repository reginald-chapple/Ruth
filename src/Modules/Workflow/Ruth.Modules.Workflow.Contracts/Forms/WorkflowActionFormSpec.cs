namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Specification for an interactive WorkflowActionForm bound to a WorkflowActionType.</summary>
/// <param name="Fields">Ordered form fields.</param>
/// <param name="Buttons">Outcome buttons rendered at the bottom of the form.</param>
/// <param name="Sections">Optional visual sections grouping fields.</param>
/// <param name="FormBuilderTemplateId">Optional reusable FormBuilderTemplate wrapper.</param>
/// <param name="HeaderMarkup">Optional merge-enabled header markup.</param>
/// <param name="FooterMarkup">Optional merge-enabled footer markup.</param>
/// <param name="AllowPersonEntry">Whether constituent contact fields are collected on the form.</param>
/// <param name="NotificationSystemCommunicationKey">Optional SystemCommunication slug triggered on task assignment.</param>
public sealed record WorkflowActionFormSpec(
    IReadOnlyList<WorkflowActionFormFieldSpec> Fields,
    IReadOnlyList<WorkflowFormButtonSpec> Buttons,
    IReadOnlyList<WorkflowActionFormSectionSpec>? Sections = null,
    Guid? FormBuilderTemplateId = null,
    string? HeaderMarkup = null,
    string? FooterMarkup = null,
    bool AllowPersonEntry = false,
    string? NotificationSystemCommunicationKey = null
);
