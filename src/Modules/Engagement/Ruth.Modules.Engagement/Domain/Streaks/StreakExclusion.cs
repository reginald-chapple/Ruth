using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Streaks;

/// <summary>Holiday, closure, or weather exclusion date forgiven by a StreakType.</summary>
public sealed class StreakExclusion : Entity<StreakExclusionId>
{
    private StreakExclusion() { }

    internal StreakExclusion(
        StreakExclusionId id,
        StreakTypeId streakTypeId,
        DateOnly exclusionDate,
        Guid? branchId
    )
        : base(id)
    {
        StreakTypeId = streakTypeId;
        ExclusionDate = exclusionDate;
        BranchId = NormalizeGuid(branchId);
    }

    public StreakTypeId StreakTypeId { get; private set; }
    public DateOnly ExclusionDate { get; private set; }
    public Guid? BranchId { get; private set; }
}
