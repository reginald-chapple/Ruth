using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Query to retrieve Streak enrollments and metrics filtered by type or person.</summary>
/// <param name="StreakTypeId">Optional StreakType filter.</param>
/// <param name="PersonId">Optional Person filter.</param>
public sealed record GetStreaksQuery(Guid? StreakTypeId = null, Guid? PersonId = null)
    : IQuery<IReadOnlyList<StreakResponse>>;
