using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Achievements;

/// <summary>Query to list configured AchievementTypes.</summary>
/// <param name="ActiveOnly">Whether to return only active achievement types.</param>
public sealed record GetAchievementTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<AchievementTypeResponse>>;
