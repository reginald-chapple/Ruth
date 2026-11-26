namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Origin or intent of a scheduled or ad-hoc Touchpoint.</summary>
public enum TouchpointKind
{
    /// <summary>Recurring outreach cadence touchpoint.</summary>
    ScheduledCadence = 0,

    /// <summary>Periodic personal / handwritten connection note.</summary>
    ConnectionNote = 1,

    /// <summary>Milestone, anniversary, or achievement celebration.</summary>
    MilestoneCelebration = 2,

    /// <summary>Wellbeing or care follow-up.</summary>
    CareFollowUp = 3,

    /// <summary>Unscheduled ad-hoc outreach.</summary>
    AdHocOutreach = 4,
}
