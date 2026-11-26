namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Event source automatically feeding a StreakType bitmask.</summary>
public enum StreakSourceKind
{
    /// <summary>Attendance recorded in a specific Group.</summary>
    GroupAttendance = 0,

    /// <summary>Attendance recorded in any Group of a specific GroupType.</summary>
    GroupTypeAttendance = 1,

    /// <summary>Any headline or program attendance / check-in.</summary>
    AnyAttendance = 2,

    /// <summary>Explicit/manual or external activity recording.</summary>
    ManualOrActivity = 3,
}
