using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Command to create an AchievementType.</summary>
/// <param name="Spec">AchievementType specification.</param>
public sealed record CreateAchievementTypeCommand(AchievementTypeSpec Spec)
    : ICommand<AchievementTypeResponse>;
