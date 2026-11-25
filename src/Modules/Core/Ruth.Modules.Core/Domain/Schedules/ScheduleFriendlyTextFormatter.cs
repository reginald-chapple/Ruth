using System.Globalization;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Produces concise, human-readable text summaries of weekly and RFC 5545 schedules.</summary>
internal static class ScheduleFriendlyTextFormatter
{
    public static string Format(ICalEventSpec? spec, DayOfWeek? weeklyDay, TimeOnly? weeklyTime)
    {
        if (spec is null)
        {
            return weeklyDay.HasValue
                ? FormatWeeklyFallback(weeklyDay.Value, weeklyTime)
                : "No schedule configured";
        }

        var timeLabel = spec.IsAllDay
            ? "All day"
            : spec.DtStart.ToString("h:mm tt", CultureInfo.InvariantCulture);

        return spec.RRule is null
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Once on {spec.DtStart:MMM d, yyyy} at {timeLabel}"
            )
            : spec.RRule.Frequency switch
            {
                RecurrenceFrequency.Daily => FormatDaily(spec.RRule, timeLabel),
                RecurrenceFrequency.Weekly => FormatWeekly(spec, spec.RRule, timeLabel),
                RecurrenceFrequency.Monthly => FormatMonthly(spec, spec.RRule, timeLabel),
                RecurrenceFrequency.Yearly => FormatYearly(spec, spec.RRule, timeLabel),
                _ => "Custom schedule",
            };
    }

    private static string FormatWeeklyFallback(DayOfWeek day, TimeOnly? time)
    {
        if (!time.HasValue)
        {
            return $"Weekly: {day}";
        }

        var formattedTime = time.Value.ToString("h:mm tt", CultureInfo.InvariantCulture);
        return $"Weekly: {day} at {formattedTime}";
    }

    private static string FormatDaily(ICalRecurrenceRule rule, string timeLabel)
    {
        return rule.Interval == 1
            ? $"Daily at {timeLabel}"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Every {rule.Interval} days at {timeLabel}"
            );
    }

    private static string FormatWeekly(
        ICalEventSpec spec,
        ICalRecurrenceRule rule,
        string timeLabel
    )
    {
        var days =
            rule.ByDays.Count > 0
                ? string.Join(", ", rule.ByDays.Select(static d => d.DayOfWeek.ToString()))
                : spec.DtStart.DayOfWeek.ToString();

        return rule.Interval == 1
            ? $"Weekly: {days} at {timeLabel}"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Every {rule.Interval} weeks on {days} at {timeLabel}"
            );
    }

    private static string FormatMonthly(
        ICalEventSpec spec,
        ICalRecurrenceRule rule,
        string timeLabel
    )
    {
        var prefix =
            rule.Interval == 1
                ? "Monthly"
                : string.Create(CultureInfo.InvariantCulture, $"Every {rule.Interval} months");

        if (rule.ByDays.Count > 0)
        {
            var dayParts = rule.ByDays.Select(FormatByDayRule);
            return $"{prefix} on the {string.Join(", ", dayParts)} at {timeLabel}";
        }

        var dayNum = rule.ByMonthDays.Count > 0 ? rule.ByMonthDays[0] : spec.DtStart.Day;
        return $"{prefix} on day {FormatOrdinal(dayNum)} at {timeLabel}";
    }

    private static string FormatYearly(
        ICalEventSpec spec,
        ICalRecurrenceRule rule,
        string timeLabel
    )
    {
        var monthNum = rule.ByMonths.Count > 0 ? rule.ByMonths[0] : spec.DtStart.Month;
        var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthNum);
        var dayNum = rule.ByMonthDays.Count > 0 ? rule.ByMonthDays[0] : spec.DtStart.Day;
        var prefix =
            rule.Interval == 1
                ? "Yearly"
                : string.Create(CultureInfo.InvariantCulture, $"Every {rule.Interval} years");
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix} on {monthName} {dayNum} at {timeLabel}"
        );
    }

    private static string FormatByDayRule(ByDayRule rule)
    {
        return rule.Ordinal.HasValue
            ? $"{FormatOrdinal(rule.Ordinal.Value)} {rule.DayOfWeek}"
            : rule.DayOfWeek.ToString();
    }

    private static string FormatOrdinal(int value)
    {
        if (value == -1)
        {
            return "last";
        }

        var abs = Math.Abs(value);
        var suffix = (abs % 100) is 11 or 12 or 13
            ? "th"
            : (abs % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        return string.Create(CultureInfo.InvariantCulture, $"{value}{suffix}");
    }
}
