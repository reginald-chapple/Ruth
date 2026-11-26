using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Evaluates and loads the active interactive entry form for a Workflow instance (with optional live draft field values).</summary>
/// <param name="WorkflowId">Target Workflow instance identifier.</param>
/// <param name="ActivityId">Optional target waiting activity identifier.</param>
/// <param name="DraftValues">Optional live draft input values for dynamic visibility recalculation.</param>
public sealed record GetWorkflowEntryFormQuery(
    Guid WorkflowId,
    Guid? ActivityId = null,
    IReadOnlyDictionary<string, string?>? DraftValues = null
) : IQuery<WorkflowEntryFormViewResponse>;
