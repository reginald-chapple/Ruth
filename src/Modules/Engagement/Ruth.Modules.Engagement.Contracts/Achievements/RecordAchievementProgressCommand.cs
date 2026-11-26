using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Command to record or advance progress on an AchievementType for a constituent.</summary>
/// <param name="AchievementTypeId">Target AchievementType identifier.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="ObservedCount">Observed cumulative or streak count.</param>
/// <param name="AsOfDate">Effective calendar date.</param>
public sealed record RecordAchievementProgressCommand(
    Guid AchievementTypeId,
    Guid PersonId,
    int ObservedCount,
    DateOnly AsOfDate
) : ICommand<AchievementAttemptResponse>;
