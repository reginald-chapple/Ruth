namespace Ruth.Modules.Groups.Domain;

/// <summary>Computes Rock-compatible Sunday week-ending dates and yyyyMMdd occurrence keys.</summary>
internal static class SundayDateCalculator
{
    /// <summary>
    /// Computes the Sunday date representing the end of the Mon..Sun ministry week containing <paramref name="date" />.
    /// Sunday maps to itself; Monday..Saturday advance to the upcoming Sunday.
    /// </summary>
    public static DateOnly ComputeSundayDate(DateOnly date)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilSunday);
    }

    /// <summary>Computes the integer yyyyMMdd key for fast warehouse/reporting joins.</summary>
    public static int ComputeDateKey(DateOnly date)
    {
        return (date.Year * 10000) + (date.Month * 100) + date.Day;
    }
}
