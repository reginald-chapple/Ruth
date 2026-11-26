namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Classification of an availability or blackout rule window.</summary>
public enum AvailabilityRuleType
{
    /// <summary>Recurring weekly or Core Schedule iCalendar window when slots are open.</summary>
    RecurringWindow = 0,

    /// <summary>Explicit one-off UTC date/time window when slots are open.</summary>
    CustomDateWindow = 1,

    /// <summary>Blackout / maintenance / provider time-off window blocking overlapping slots.</summary>
    BlackoutWindow = 2,
}
