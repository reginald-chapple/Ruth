namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Complete runtime form payload rendered in WorkflowEntry and MyTasks UI.</summary>
/// <param name="WorkflowId">Owning Workflow instance identifier.</param>
/// <param name="WorkflowNumber">Formatted workflow number.</param>
/// <param name="WorkflowName">Workflow instance title.</param>
/// <param name="StateLabel">Current state label.</param>
/// <param name="ActivityId">Active waiting WorkflowActivity identifier.</param>
/// <param name="ActivityName">Active waiting activity display name.</param>
/// <param name="ActionId">Waiting WorkflowAction instance identifier.</param>
/// <param name="ActionTypeId">Owning WorkflowActionType identifier.</param>
/// <param name="RenderedHeaderHtml">Merge-rendered header HTML.</param>
/// <param name="RenderedFooterHtml">Merge-rendered footer HTML.</param>
/// <param name="AllowPersonEntry">Whether constituent contact fields are shown.</param>
/// <param name="Sections">Evaluated sections.</param>
/// <param name="UngroupedFields">Evaluated fields not assigned to any section.</param>
/// <param name="Buttons">Available outcome action buttons.</param>
public sealed record WorkflowEntryFormViewResponse(
    Guid WorkflowId,
    string WorkflowNumber,
    string WorkflowName,
    string StateLabel,
    Guid ActivityId,
    string ActivityName,
    Guid ActionId,
    Guid ActionTypeId,
    string? RenderedHeaderHtml,
    string? RenderedFooterHtml,
    bool AllowPersonEntry,
    IReadOnlyList<WorkflowEntrySectionViewDto> Sections,
    IReadOnlyList<WorkflowEntryFieldViewDto> UngroupedFields,
    IReadOnlyList<WorkflowFormButtonSpec> Buttons
);
