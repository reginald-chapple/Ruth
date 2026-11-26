namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Metadata describing a registered modular IWorkflowAction plugin.</summary>
/// <param name="ActionKey">Unique action plugin key.</param>
/// <param name="DisplayName">Human-readable plugin name.</param>
/// <param name="Category">Functional action category (Control Flow, Attributes, Templates, CRM, Groups, Finance, Engagement, Communication, Integrations, Forms).</param>
/// <param name="Description">Concise description of configuration parameters and behavior.</param>
public sealed record WorkflowActionPluginDescriptor(
    string ActionKey,
    string DisplayName,
    string Category,
    string Description
);
