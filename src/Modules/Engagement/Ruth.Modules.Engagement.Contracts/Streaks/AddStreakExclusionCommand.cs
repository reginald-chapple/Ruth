using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Command to add an excluded holiday/closure date to a StreakType and recompute affected streaks.</summary>
/// <param name="StreakTypeId">Target StreakType identifier.</param>
/// <param name="ExclusionDate">Excluded calendar date.</param>
/// <param name="BranchId">Optional Branch scope.</param>
public sealed record AddStreakExclusionCommand(
    Guid StreakTypeId,
    DateOnly ExclusionDate,
    Guid? BranchId = null
) : ICommand<StreakTypeResponse>;
