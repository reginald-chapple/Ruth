namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Staffing health relative to GroupLocationScheduleConfig capacity thresholds.</summary>
public enum ScheduleCapacityStatus
{
    /// <summary>No capacity thresholds configured.</summary>
    Unconfigured = 0,

    /// <summary>Confirmed/requested count is below MinimumCapacity.</summary>
    UnderMinimum = 1,

    /// <summary>Meets MinimumCapacity but is below DesiredCapacity.</summary>
    BelowDesired = 2,

    /// <summary>Meets DesiredCapacity and is within MaximumCapacity.</summary>
    Optimal = 3,

    /// <summary>Reached MaximumCapacity.</summary>
    AtMaximum = 4,

    /// <summary>Exceeds MaximumCapacity.</summary>
    OverMaximum = 5,
}
