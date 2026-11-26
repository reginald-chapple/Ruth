namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Conversion goal classifications tracked by a CommunicationFlow.</summary>
public enum FlowConversionGoalKind
{
    /// <summary>Informational sequence with no conversion goal.</summary>
    None = 0,

    /// <summary>Recipient joined a target Group or cohort.</summary>
    JoinedTargetGroup = 1,

    /// <summary>Recipient attended a session or checked in within the conversion window.</summary>
    AttendedSession = 2,

    /// <summary>Recipient clicked a URL matching the configured goal prefix.</summary>
    ClickedGoalUrl = 3,

    /// <summary>Conversion recorded explicitly via API, workflow, or external webhook.</summary>
    ManualOrExternalGoal = 4,
}
