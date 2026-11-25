namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Classification of how a schedule defines its recurrence.</summary>
public enum ScheduleKind
{
    /// <summary>No recurrence configured.</summary>
    None = 0,

    /// <summary>Simple weekly day-of-week and time-of-day schedule.</summary>
    Weekly = 1,

    /// <summary>Nameless/inline custom iCalendar schedule.</summary>
    Custom = 2,

    /// <summary>Named reusable iCalendar schedule.</summary>
    Named = 3,
}
