using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Modular strategy contract for workflow step execution plugins.</summary>
public interface IWorkflowAction
{
    /// <summary>Gets the unique plugin key (for example, workflow.attributes.set-value).</summary>
    string ActionKey { get; }

    /// <summary>Gets the human-readable plugin display name.</summary>
    string DisplayName { get; }

    /// <summary>Gets the functional category for UI grouping.</summary>
    string Category { get; }

    /// <summary>Gets a concise summary of configuration keys and side effects.</summary>
    string Description { get; }

    /// <summary>Executes the workflow action against the supplied runtime context.</summary>
    Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    );
}
