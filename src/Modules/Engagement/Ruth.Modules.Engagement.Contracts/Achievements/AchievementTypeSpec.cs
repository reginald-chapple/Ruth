namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Specification for creating or updating an AchievementType.</summary>
/// <param name="Name">Achievement display name.</param>
/// <param name="Code">Unique achievement code.</param>
/// <param name="SourceKind">Driving metric source kind.</param>
/// <param name="TargetCount">Target threshold count (&gt;= 1).</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Core Category ID.</param>
/// <param name="TargetStreakTypeId">Optional linked StreakType ID.</param>
/// <param name="TargetJourneyProgramId">Optional linked JourneyProgram ID.</param>
/// <param name="TargetMilestoneTypeId">Optional linked MilestoneType ID.</param>
/// <param name="AllowOverAchievement">Whether progress may exceed 100%.</param>
/// <param name="MaxSuccessesAllowed">Optional cap on earned attempts (null = unlimited).</param>
/// <param name="PrerequisiteAchievementIds">Required prerequisite AchievementType IDs.</param>
/// <param name="BadgeIconCssClass">Optional badge icon class.</param>
/// <param name="HighlightColorHex">Optional highlight hex color.</param>
/// <param name="CelebrationSystemCommunicationKey">Optional transactional celebration template key.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="IsPublic">Whether visible publicly.</param>
public sealed record AchievementTypeSpec(
    string Name,
    string Code,
    AchievementSourceKind SourceKind,
    int TargetCount,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? TargetStreakTypeId = null,
    Guid? TargetJourneyProgramId = null,
    Guid? TargetMilestoneTypeId = null,
    bool AllowOverAchievement = false,
    int? MaxSuccessesAllowed = 1,
    IReadOnlyList<Guid>? PrerequisiteAchievementIds = null,
    string? BadgeIconCssClass = null,
    string? HighlightColorHex = null,
    string? CelebrationSystemCommunicationKey = null,
    bool IsActive = true,
    bool IsPublic = true
);
