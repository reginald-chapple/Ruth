namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Represents a constituent's bitmask Streak metrics and recent cadence sparkline.</summary>
/// <param name="Id">Streak identifier.</param>
/// <param name="StreakTypeId">StreakType identifier.</param>
/// <param name="StreakTypeName">StreakType display name.</param>
/// <param name="Cadence">Daily or Weekly cadence.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="EnrollmentDate">Enrollment date.</param>
/// <param name="EngagementCount">Total engaged buckets recorded in the bitmask.</param>
/// <param name="CurrentStreakCount">Current active consecutive bucket count.</param>
/// <param name="CurrentStreakStartDate">Optional start date of current active streak.</param>
/// <param name="LongestStreakCount">All-time longest consecutive bucket count.</param>
/// <param name="LongestStreakStartDate">Optional start date of longest streak.</param>
/// <param name="LongestStreakEndDate">Optional end date of longest streak.</param>
/// <param name="MostRecentEngagementDate">Optional most recent engaged bucket date.</param>
/// <param name="RecentBucketsBitString">Recent 24-bucket participation string ('1'=engaged, 'E'=excluded, '0'=missed).</param>
public sealed record StreakResponse(
    Guid Id,
    Guid StreakTypeId,
    string StreakTypeName,
    StreakCadence Cadence,
    Guid PersonId,
    Guid? BranchId,
    DateOnly EnrollmentDate,
    int EngagementCount,
    int CurrentStreakCount,
    DateOnly? CurrentStreakStartDate,
    int LongestStreakCount,
    DateOnly? LongestStreakStartDate,
    DateOnly? LongestStreakEndDate,
    DateOnly? MostRecentEngagementDate,
    string RecentBucketsBitString
);
