using Ruth.Modules.Workflow.Contracts.Forms;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Validated outcome produced by FormVisibilityEvaluator when a form is submitted.</summary>
/// <param name="SelectedButton">Chosen outcome button specification.</param>
/// <param name="SanitizedAttributes">Validated visible, writable field values to persist onto the workflow.</param>
public sealed record FormSubmissionEvaluationResult(
    WorkflowFormButtonSpec SelectedButton,
    IReadOnlyDictionary<string, string?> SanitizedAttributes
);
