using Ruth.Modules.Groups.Contracts.Scheduling;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Pure domain service evaluating volunteer staffing thresholds and scheduling eligibility.</summary>
internal static class ServingScheduleEvaluator
{
    /// <summary>Computes slot staffing health relative to min/desired/max volunteer thresholds.</summary>
    public static ScheduleCapacityStatus EvaluateCapacityStatus(
        int activeScheduledCount,
        int? minimumCapacity,
        int? desiredCapacity,
        int? maximumCapacity
    )
    {
        return !minimumCapacity.HasValue && !desiredCapacity.HasValue && !maximumCapacity.HasValue
                ? ScheduleCapacityStatus.Unconfigured
            : maximumCapacity.HasValue && activeScheduledCount > maximumCapacity.Value
                ? ScheduleCapacityStatus.OverMaximum
            : maximumCapacity.HasValue && activeScheduledCount == maximumCapacity.Value
                ? ScheduleCapacityStatus.AtMaximum
            : minimumCapacity.HasValue && activeScheduledCount < minimumCapacity.Value
                ? ScheduleCapacityStatus.UnderMinimum
            : desiredCapacity.HasValue && activeScheduledCount < desiredCapacity.Value
                ? ScheduleCapacityStatus.BelowDesired
            : ScheduleCapacityStatus.Optimal;
    }

    /// <summary>Returns true if adding one more volunteer would exceed MaximumCapacity.</summary>
    public static bool WouldExceedMaximumCapacity(int currentActiveCount, int? maximumCapacity)
    {
        return maximumCapacity.HasValue && currentActiveCount + 1 > maximumCapacity.Value;
    }

    /// <summary>Determines whether a volunteer candidate is eligible without conflicts.</summary>
    public static bool IsCandidateEligible(
        bool groupSchedulingEnabled,
        bool dateExcludedByGroupType,
        bool hasPersonBlackout,
        bool hasRequirementGap,
        bool hasDoubleBooking
    )
    {
        return groupSchedulingEnabled
            && !dateExcludedByGroupType
            && !hasPersonBlackout
            && !hasRequirementGap
            && !hasDoubleBooking;
    }
}
