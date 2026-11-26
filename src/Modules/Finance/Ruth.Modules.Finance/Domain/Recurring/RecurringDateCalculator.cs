using Ruth.Modules.Finance.Contracts.Pledges;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>First-principles recurring billing date calculator preserving anchor day-of-month across variable month lengths.</summary>
public static class RecurringDateCalculator
{
    /// <summary>Computes the next scheduled billing date after <paramref name="currentDate" /> preserving <paramref name="anchorDayOfMonth" />.</summary>
    public static DateOnly Advance(
        DateOnly currentDate,
        PledgeFrequency frequency,
        int anchorDayOfMonth
    )
    {
        return frequency switch
        {
            PledgeFrequency.OneTime => currentDate,
            PledgeFrequency.Weekly => currentDate.AddDays(7),
            PledgeFrequency.BiWeekly => currentDate.AddDays(14),
            PledgeFrequency.SemiMonthly => AdvanceSemiMonthly(currentDate),
            PledgeFrequency.Monthly => AddAnchoredMonths(currentDate, 1, anchorDayOfMonth),
            PledgeFrequency.Quarterly => AddAnchoredMonths(currentDate, 3, anchorDayOfMonth),
            PledgeFrequency.Annually => AddAnchoredMonths(currentDate, 12, anchorDayOfMonth),
            _ => currentDate.AddMonths(1),
        };
    }

    private static DateOnly AdvanceSemiMonthly(DateOnly current)
    {
        if (current.Day < 15)
        {
            return new DateOnly(current.Year, current.Month, 15);
        }

        var nextMonth = current.AddMonths(1);
        return new DateOnly(nextMonth.Year, nextMonth.Month, 1);
    }

    private static DateOnly AddAnchoredMonths(DateOnly current, int monthsToAdd, int anchorDay)
    {
        var shifted = new DateOnly(current.Year, current.Month, 1).AddMonths(monthsToAdd);
        var maxDay = DateTime.DaysInMonth(shifted.Year, shifted.Month);
        var clampedDay = Math.Clamp(anchorDay, 1, maxDay);
        return new DateOnly(shifted.Year, shifted.Month, clampedDay);
    }
}
