namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Lifecycle status of a CommunicationFlowInstance cohort run.</summary>
public enum FlowInstanceStatus
{
    /// <summary>Actively progressing enrolled recipients through timed steps.</summary>
    Active = 0,

    /// <summary>Temporarily paused by staff.</summary>
    Paused = 1,

    /// <summary>All enrolled recipients finished or exited.</summary>
    Completed = 2,

    /// <summary>Permanently cancelled before completion.</summary>
    Cancelled = 3,
}
