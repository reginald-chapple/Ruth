namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Lifecycle state of an EngagementRequest.</summary>
public enum EngagementRequestState
{
    /// <summary>Actively worked on the Connector Board.</summary>
    Active = 0,

    /// <summary>Snoozed until a scheduled future follow-up date.</summary>
    FutureFollowUp = 1,

    /// <summary>Successfully connected / placed.</summary>
    Connected = 2,

    /// <summary>Closed without connection.</summary>
    Inactive = 3,
}
