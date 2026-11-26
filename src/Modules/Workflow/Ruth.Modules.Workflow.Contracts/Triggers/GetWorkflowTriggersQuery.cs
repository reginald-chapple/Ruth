using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Lists configured WorkflowTrigger automation rules.</summary>
/// <param name="IncludeInactive">When true, includes disabled triggers.</param>
public sealed record GetWorkflowTriggersQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<WorkflowTriggerResponse>>;
