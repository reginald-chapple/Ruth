using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Query to list configured StreakTypes.</summary>
/// <param name="ActiveOnly">Whether to return only active streak types.</param>
public sealed record GetStreakTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<StreakTypeResponse>>;
