using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Command to mark a participation date in a constituent's Streak bitmask and evaluate achievements.</summary>
/// <param name="StreakTypeId">Target StreakType identifier.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="EngagementDate">Participation calendar date.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
public sealed record RecordStreakEngagementCommand(
    Guid StreakTypeId,
    Guid PersonId,
    DateOnly EngagementDate,
    Guid? BranchId = null
) : ICommand<StreakResponse>;
