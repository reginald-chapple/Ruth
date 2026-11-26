using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Command to update an AchievementType.</summary>
/// <param name="AchievementTypeId">AchievementType identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateAchievementTypeCommand(Guid AchievementTypeId, AchievementTypeSpec Spec)
    : ICommand<AchievementTypeResponse>;
