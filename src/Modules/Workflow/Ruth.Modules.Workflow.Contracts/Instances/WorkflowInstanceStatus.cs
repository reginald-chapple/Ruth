namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Lifecycle state of a Workflow instance.</summary>
public enum WorkflowInstanceStatus
{
    /// <summary>Created and queued for initial processing.</summary>
    Pending = 0,

    /// <summary>Actively executing activities and actions.</summary>
    Active = 1,

    /// <summary>Paused waiting on an interactive user entry form submission.</summary>
    WaitingForInput = 2,

    /// <summary>Suspended until a scheduled resume timestamp.</summary>
    Suspended = 3,

    /// <summary>Successfully completed all active activities.</summary>
    Completed = 4,

    /// <summary>Stopped after exceeding maximum processing retries or fatal action failure.</summary>
    Faulted = 5,

    /// <summary>Explicitly cancelled by an actor or control-flow action.</summary>
    Cancelled = 6,
}
