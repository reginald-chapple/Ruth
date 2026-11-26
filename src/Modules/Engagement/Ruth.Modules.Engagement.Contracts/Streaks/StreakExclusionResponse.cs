namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Represents an excluded holiday/closure date on a StreakType.</summary>
/// <param name="Id">Exclusion identifier.</param>
/// <param name="StreakTypeId">Owning StreakType identifier.</param>
/// <param name="ExclusionDate">Calendar date excluded from breaking streaks.</param>
/// <param name="BranchId">Optional Branch scope (null applies organization-wide).</param>
public sealed record StreakExclusionResponse(
    Guid Id,
    Guid StreakTypeId,
    DateOnly ExclusionDate,
    Guid? BranchId
);
