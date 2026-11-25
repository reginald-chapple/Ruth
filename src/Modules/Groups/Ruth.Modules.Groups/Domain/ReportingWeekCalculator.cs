namespace Ruth.Modules.Groups.Domain;

/// <summary>Computes configurable reporting week-ending dates and integer yyyyMMdd occurrence keys.</summary>
internal static class ReportingWeekCalculator
{
    /// <summary>
    /// Computes the calendar date representing the end of the 7-day reporting week containing <paramref name="date" />.
    /// Defaults to ISO-8601 week ending (<see cref="DayOfWeek.Sunday" />), while supporting any organization week-end day.
    /// </summary>
    public static DateOnly ComputeWeekEndDate(
        DateOnly date,
        DayOfWeek weekEndDay = DayOfWeek.Sunday
    )
    {
        var daysUntilEnd = ((int)weekEndDay - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilEnd);
    }

    /// <summary>Computes the integer yyyyMMdd key for fast warehouse/reporting joins.</summary>
    public static int ComputeDateKey(DateOnly date)
    {
        return (date.Year * 10000) + (date.Month * 100) + date.Day;
    }
}
