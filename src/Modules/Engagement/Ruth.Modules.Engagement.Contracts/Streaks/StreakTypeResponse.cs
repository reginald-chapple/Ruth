namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Represents a configured StreakType and its exclusion dates.</summary>
/// <param name="Id">StreakType identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Unique code.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Cadence">Bucket cadence.</param>
/// <param name="WeekEndDay">Reporting week-end day.</param>
/// <param name="AnchorStartDate">Anchor bucket 0 date.</param>
/// <param name="SourceKind">Event source kind.</param>
/// <param name="TargetEntityId">Optional target Group or GroupType ID.</param>
/// <param name="RequiresExplicitEnrollment">Whether explicit enrollment is required.</param>
/// <param name="EnableAchievements">Whether streak achievements are evaluated.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Exclusions">Configured exclusion dates.</param>
public sealed record StreakTypeResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    StreakCadence Cadence,
    DayOfWeek WeekEndDay,
    DateOnly AnchorStartDate,
    StreakSourceKind SourceKind,
    Guid? TargetEntityId,
    bool RequiresExplicitEnrollment,
    bool EnableAchievements,
    bool IsActive,
    IReadOnlyList<StreakExclusionResponse> Exclusions
);
