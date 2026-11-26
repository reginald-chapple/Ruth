using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain.Streaks;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal static class StreakProjectionMapper
{
    public static StreakTypeResponse ToResponse(StreakType entity)
    {
        return new StreakTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.Code,
            entity.Description,
            entity.Cadence,
            entity.WeekEndDay,
            entity.AnchorStartDate,
            entity.SourceKind,
            entity.TargetEntityId,
            entity.RequiresExplicitEnrollment,
            entity.EnableAchievements,
            entity.IsActive,
            [
                .. entity
                    .Exclusions.OrderBy(static e => e.ExclusionDate)
                    .Select(static e => new StreakExclusionResponse(
                        e.Id.Value,
                        e.StreakTypeId.Value,
                        e.ExclusionDate,
                        e.BranchId
                    )),
            ]
        );
    }

    public static StreakResponse ToResponse(
        Streak streak,
        StreakType streakType,
        DateOnly today
    )
    {
        var asOfBucket = Math.Max(
            (streak.EngagementMap.Length * 8) - 1,
            StreakBitmaskEngine.ComputeBucketIndex(
                streakType.AnchorStartDate,
                today,
                streakType.Cadence,
                streakType.WeekEndDay
            )
        );
        var exclusionMap = StreakBitmaskEngine.BuildExclusionMap(
            streakType.AnchorStartDate,
            streakType.Cadence,
            streakType.WeekEndDay,
            streakType.Exclusions,
            streak.BranchId,
            asOfBucket
        );
        var recent = StreakBitmaskEngine.FormatRecentBuckets(
            streak.EngagementMap,
            exclusionMap,
            asOfBucket
        );

        return new StreakResponse(
            streak.Id.Value,
            streak.StreakTypeId,
            streakType.Name,
            streakType.Cadence,
            streak.PersonId,
            streak.BranchId,
            streak.EnrollmentDate,
            streak.EngagementCount,
            streak.CurrentStreakCount,
            streak.CurrentStreakStartDate,
            streak.LongestStreakCount,
            streak.LongestStreakStartDate,
            streak.LongestStreakEndDate,
            streak.MostRecentEngagementDate,
            recent
        );
    }
}
