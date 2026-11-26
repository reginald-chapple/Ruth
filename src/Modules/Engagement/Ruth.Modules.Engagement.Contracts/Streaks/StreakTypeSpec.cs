namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Specification for creating or updating a StreakType.</summary>
/// <param name="Name">Streak type display name.</param>
/// <param name="Code">Unique uppercase code.</param>
/// <param name="Cadence">Daily or Weekly cadence.</param>
/// <param name="AnchorStartDate">Bucket 0 start date.</param>
/// <param name="Description">Optional description.</param>
/// <param name="WeekEndDay">Reporting week-ending day when Cadence is Weekly.</param>
/// <param name="SourceKind">Attendance/activity event source.</param>
/// <param name="TargetEntityId">Optional target GroupId or GroupTypeId.</param>
/// <param name="RequiresExplicitEnrollment">Whether people must be enrolled before attendance marks bits.</param>
/// <param name="EnableAchievements">Whether streak milestones evaluate AchievementTypes.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record StreakTypeSpec(
    string Name,
    string Code,
    StreakCadence Cadence,
    DateOnly AnchorStartDate,
    string? Description = null,
    DayOfWeek WeekEndDay = DayOfWeek.Sunday,
    StreakSourceKind SourceKind = StreakSourceKind.AnyAttendance,
    Guid? TargetEntityId = null,
    bool RequiresExplicitEnrollment = false,
    bool EnableAchievements = true,
    bool IsActive = true
);
