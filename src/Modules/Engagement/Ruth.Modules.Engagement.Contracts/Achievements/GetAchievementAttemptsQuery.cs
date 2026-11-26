using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Query to retrieve AchievementAttempts filtered by achievement type, person, or success state.</summary>
/// <param name="AchievementTypeId">Optional AchievementType filter.</param>
/// <param name="PersonId">Optional Person filter.</param>
/// <param name="SuccessfulOnly">Whether to return only earned/successful attempts.</param>
public sealed record GetAchievementAttemptsQuery(
    Guid? AchievementTypeId = null,
    Guid? PersonId = null,
    bool SuccessfulOnly = false
) : IQuery<IReadOnlyList<AchievementAttemptResponse>>;
