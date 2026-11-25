namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Supported RFC 5545 RRULE recurrence frequencies.</summary>
public enum RecurrenceFrequency
{
    /// <summary>Repeats every N days.</summary>
    Daily = 0,

    /// <summary>Repeats every N weeks on specified days of the week.</summary>
    Weekly = 1,

    /// <summary>Repeats every N months by month day or weekday ordinal.</summary>
    Monthly = 2,

    /// <summary>Repeats every N years by month/day or weekday ordinal.</summary>
    Yearly = 3,
}
