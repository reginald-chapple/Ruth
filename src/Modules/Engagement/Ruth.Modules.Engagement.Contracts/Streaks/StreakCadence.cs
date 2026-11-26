namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Bucket granularity for a StreakType participation bitmask.</summary>
public enum StreakCadence
{
    /// <summary>Each bit represents a single calendar day.</summary>
    Daily = 0,

    /// <summary>Each bit represents a 7-day reporting week aligned to WeekEndDay.</summary>
    Weekly = 1,
}
