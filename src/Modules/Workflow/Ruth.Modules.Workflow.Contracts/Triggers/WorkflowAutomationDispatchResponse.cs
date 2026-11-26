namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Result of evaluating WorkflowTrigger rules for an incoming stimulus.</summary>
/// <param name="EvaluatedTriggerCount">Number of active triggers inspected.</param>
/// <param name="LaunchedWorkflowIds">Identifiers of newly launched Workflow instances.</param>
public sealed record WorkflowAutomationDispatchResponse(
    int EvaluatedTriggerCount,
    IReadOnlyList<Guid> LaunchedWorkflowIds
);
