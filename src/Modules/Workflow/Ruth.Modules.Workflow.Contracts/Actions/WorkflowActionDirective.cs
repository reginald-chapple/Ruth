namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Structured outcome emitted by an IWorkflowAction plugin execution.</summary>
/// <param name="OutcomeKind">Flow-control outcome directive.</param>
/// <param name="Summary">Human-readable execution log summary.</param>
/// <param name="UpdatedWorkflowAttributes">Optional workflow attribute mutations to merge.</param>
/// <param name="UpdatedActivityAttributes">Optional activity attribute mutations to merge.</param>
/// <param name="NewStateLabel">Optional workflow state label update.</param>
/// <param name="TargetActivityKey">Optional target activity key when OutcomeKind is ActivateActivity.</param>
/// <param name="ResumeAtUtc">Optional UTC timestamp when OutcomeKind is DelayUntil.</param>
/// <param name="AssignPersonId">Optional person assignee update on the active activity.</param>
/// <param name="AssignGroupId">Optional group assignee update on the active activity.</param>
public sealed record WorkflowActionDirective(
    WorkflowActionOutcomeKind OutcomeKind,
    string Summary,
    IReadOnlyDictionary<string, string?>? UpdatedWorkflowAttributes = null,
    IReadOnlyDictionary<string, string?>? UpdatedActivityAttributes = null,
    string? NewStateLabel = null,
    string? TargetActivityKey = null,
    DateTimeOffset? ResumeAtUtc = null,
    Guid? AssignPersonId = null,
    Guid? AssignGroupId = null
);
