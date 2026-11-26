namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Represents an in-progress or completed AchievementAttempt for a constituent.</summary>
/// <param name="Id">AchievementAttempt identifier.</param>
/// <param name="AchievementTypeId">AchievementType identifier.</param>
/// <param name="AchievementName">Achievement display name.</param>
/// <param name="AchievementCode">Achievement code.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="AttemptStartDate">Attempt start date.</param>
/// <param name="AttemptEndDate">Optional completion/close date.</param>
/// <param name="CurrentCount">Accumulated count toward target.</param>
/// <param name="TargetCountSnapshot">Target count snapshot.</param>
/// <param name="Progress">Normalized progress ratio (e.g. 0.0 to 1.0+).</param>
/// <param name="IsClosed">Whether the attempt is closed.</param>
/// <param name="IsSuccessful">Whether the target threshold was achieved.</param>
public sealed record AchievementAttemptResponse(
    Guid Id,
    Guid AchievementTypeId,
    string AchievementName,
    string AchievementCode,
    Guid PersonId,
    DateOnly AttemptStartDate,
    DateOnly? AttemptEndDate,
    int CurrentCount,
    int TargetCountSnapshot,
    decimal Progress,
    bool IsClosed,
    bool IsSuccessful
);
