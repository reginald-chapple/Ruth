using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Processes due Pending, Active, and scheduled Suspended workflows in a bounded batch.</summary>
/// <param name="MaxBatchSize">Maximum number of workflow instances to evaluate in this pass.</param>
public sealed record ProcessPendingWorkflowsCommand(int MaxBatchSize = 50)
    : ICommand<WorkflowBatchProcessResponse>;
