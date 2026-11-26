namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Event metric feeding an AchievementType badge or certification.</summary>
public enum AchievementSourceKind
{
    /// <summary>Triggered by reaching a Streak current/longest count.</summary>
    StreakCount = 0,

    /// <summary>Triggered by cumulative attendance engagements.</summary>
    CumulativeAttendance = 1,

    /// <summary>Triggered by completing JourneyProgram milestones.</summary>
    MilestoneCompletions = 2,

    /// <summary>Triggered by completing entire JourneyPrograms.</summary>
    JourneyCompletions = 3,

    /// <summary>Triggered by Connected pipeline requests.</summary>
    EngagementConnections = 4,

    /// <summary>Triggered by manual or external progress updates.</summary>
    ManualOrExternal = 5,
}
