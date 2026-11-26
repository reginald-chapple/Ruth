namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Lifecycle status of a Touchpoint.</summary>
public enum TouchpointStatus
{
    /// <summary>Scheduled and awaiting outreach.</summary>
    Pending = 0,

    /// <summary>Deferred to a later date.</summary>
    Snoozed = 1,

    /// <summary>Outreach completed and logged.</summary>
    Completed = 2,

    /// <summary>Intentionally skipped for this cycle.</summary>
    Skipped = 3,

    /// <summary>Cancelled.</summary>
    Cancelled = 4,
}
