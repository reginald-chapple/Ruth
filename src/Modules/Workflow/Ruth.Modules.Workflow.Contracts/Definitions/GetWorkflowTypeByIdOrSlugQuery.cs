using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Loads a single WorkflowType definition by UUID or slug.</summary>
/// <param name="IdOrSlug">WorkflowType UUID or slug.</param>
public sealed record GetWorkflowTypeByIdOrSlugQuery(string IdOrSlug) : IQuery<WorkflowTypeResponse>;
