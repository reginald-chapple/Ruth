using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Lists WorkflowType definitions ordered by name.</summary>
/// <param name="IncludeInactive">When true, includes inactive WorkflowTypes.</param>
public sealed record GetWorkflowTypesQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<WorkflowTypeResponse>>;
