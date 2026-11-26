namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Computed SLA and activity health indicator for an EngagementRequest.</summary>
public enum EngagementRequestHealth
{
    /// <summary>Active and within expected response window.</summary>
    OnTrack = 0,

    /// <summary>Approaching idle threshold or upcoming follow-up date.</summary>
    DueSoon = 1,

    /// <summary>No recorded activity within the stage/pipeline idle threshold.</summary>
    Idle = 2,

    /// <summary>Scheduled follow-up date is today or overdue.</summary>
    OverdueFollowUp = 3,

    /// <summary>Request is Connected.</summary>
    Completed = 4,

    /// <summary>Request is Inactive.</summary>
    Inactive = 5,
}
