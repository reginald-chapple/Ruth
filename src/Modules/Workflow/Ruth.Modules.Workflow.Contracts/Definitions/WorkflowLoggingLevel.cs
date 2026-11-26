namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Granularity of persisted audit log entries emitted during workflow execution.</summary>
public enum WorkflowLoggingLevel
{
    /// <summary>Do not persist execution log rows except terminal errors.</summary>
    None = 0,

    /// <summary>Persist lifecycle transitions at the workflow instance level.</summary>
    Workflow = 1,

    /// <summary>Persist workflow and activity lifecycle transitions.</summary>
    Activity = 2,

    /// <summary>Persist workflow, activity, and individual action execution entries.</summary>
    Action = 3,
}
