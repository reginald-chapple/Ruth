namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Summary of a background workflow processing batch run.</summary>
/// <param name="EvaluatedCount">Number of workflow instances inspected.</param>
/// <param name="CompletedCount">Number of instances that reached Completed during this pass.</param>
/// <param name="WaitingCount">Number of instances paused in WaitingForInput or Suspended.</param>
/// <param name="FaultedCount">Number of instances marked Faulted.</param>
public sealed record WorkflowBatchProcessResponse(
    int EvaluatedCount,
    int CompletedCount,
    int WaitingCount,
    int FaultedCount
);
