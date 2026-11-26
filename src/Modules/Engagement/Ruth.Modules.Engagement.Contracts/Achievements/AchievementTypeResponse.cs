namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Represents a configured AchievementType badge or certification.</summary>
/// <param name="Id">AchievementType identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Unique code.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Category ID.</param>
/// <param name="SourceKind">Driving metric source.</param>
/// <param name="TargetCount">Target threshold count.</param>
/// <param name="TargetStreakTypeId">Optional StreakType ID.</param>
/// <param name="TargetJourneyProgramId">Optional JourneyProgram ID.</param>
/// <param name="TargetMilestoneTypeId">Optional MilestoneType ID.</param>
/// <param name="AllowOverAchievement">Whether over-achievement is enabled.</param>
/// <param name="MaxSuccessesAllowed">Optional maximum successes cap.</param>
/// <param name="PrerequisiteAchievementIds">Prerequisite AchievementType IDs.</param>
/// <param name="BadgeIconCssClass">Optional badge icon class.</param>
/// <param name="HighlightColorHex">Optional highlight hex color.</param>
/// <param name="CelebrationSystemCommunicationKey">Optional celebration notification key.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="IsPublic">Whether public.</param>
public sealed record AchievementTypeResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid? CategoryId,
    AchievementSourceKind SourceKind,
    int TargetCount,
    Guid? TargetStreakTypeId,
    Guid? TargetJourneyProgramId,
    Guid? TargetMilestoneTypeId,
    bool AllowOverAchievement,
    int? MaxSuccessesAllowed,
    IReadOnlyList<Guid> PrerequisiteAchievementIds,
    string? BadgeIconCssClass,
    string? HighlightColorHex,
    string? CelebrationSystemCommunicationKey,
    bool IsActive,
    bool IsPublic
);
