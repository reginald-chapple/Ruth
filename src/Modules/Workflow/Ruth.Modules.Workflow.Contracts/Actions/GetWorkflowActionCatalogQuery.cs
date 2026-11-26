using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Queries all registered IWorkflowAction plugins available in the host.</summary>
public sealed record GetWorkflowActionCatalogQuery
    : IQuery<IReadOnlyList<WorkflowActionPluginDescriptor>>;
