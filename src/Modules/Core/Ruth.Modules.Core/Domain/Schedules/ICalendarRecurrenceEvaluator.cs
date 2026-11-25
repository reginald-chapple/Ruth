using Ruth.Modules.Core.Contracts.Schedules;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Evaluates RFC 5545 recurrence rules with DST, leap-year, BYDAY/BYSETPOS, RDATE/EXDATE, and exclusion support.</summary>
internal static class ICalendarRecurrenceEvaluator
{
    private const int MaxEvaluatedOccurrences = 50_000;

    public static IReadOnlyList<ScheduleOccurrenceDto> Evaluate(
        ICalEventSpec spec,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        TimeZoneInfo timeZone,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? categoryExclusions = null
    )
    {
        if (rangeEndUtc < rangeStartUtc)
        {
            return [];
        }

        var localRangeEnd = ICalendarTimeZoneHelper.ToLocalWallTime(rangeEndUtc, timeZone);
        var hardEndLocal = spec.DtStart.AddYears(100);
        var effectiveEndLocal = localRangeEnd < hardEndLocal ? localRangeEnd : hardEndLocal;

        var candidates = CollectCandidateLocalStarts(spec, effectiveEndLocal, timeZone);
        var excludedSet = BuildExDateSet(spec.ExDates);

        var results = new List<ScheduleOccurrenceDto>();
        foreach (var localStart in candidates)
        {
            if (IsExcludedByExDate(localStart, excludedSet, spec.IsAllDay))
            {
                continue;
            }

            var startUtc = ICalendarTimeZoneHelper.ToUtcOffset(
                localStart,
                timeZone,
                spec.DtStartIsUtc
            );
            var endUtc = startUtc.Add(spec.Duration);
            if (startUtc < rangeStartUtc || startUtc > rangeEndUtc)
            {
                continue;
            }

            if (IsExcludedByCategory(startUtc, categoryExclusions))
            {
                continue;
            }

            var actualLocalStart = ICalendarTimeZoneHelper.ToLocalWallTime(startUtc, timeZone);
            var actualLocalEnd = ICalendarTimeZoneHelper.ToLocalWallTime(endUtc, timeZone);
            var dateKey =
                (actualLocalStart.Year * 10000)
                + (actualLocalStart.Month * 100)
                + actualLocalStart.Day;
            results.Add(
                new ScheduleOccurrenceDto(
                    startUtc,
                    endUtc,
                    actualLocalStart,
                    actualLocalEnd,
                    dateKey
                )
            );
        }

        return results;
    }

    private static SortedSet<DateTime> CollectCandidateLocalStarts(
        ICalEventSpec spec,
        DateTime effectiveEndLocal,
        TimeZoneInfo timeZone
    )
    {
        var results = new SortedSet<DateTime>();
        if (spec.RRule is null)
        {
            _ = results.Add(spec.DtStart);
        }
        else
        {
            var generated = ExpandRule(spec.DtStart, spec.RRule, effectiveEndLocal, timeZone);
            foreach (var dt in generated)
            {
                _ = results.Add(dt);
            }
        }

        foreach (var rDate in spec.RDates)
        {
            var merged =
                rDate.TimeOfDay == TimeSpan.Zero && !spec.IsAllDay
                    ? rDate.Date.Add(spec.DtStart.TimeOfDay)
                    : rDate;
            _ = results.Add(merged);
        }

        return results;
    }

    private static List<DateTime> ExpandRule(
        DateTime dtStart,
        ICalRecurrenceRule rule,
        DateTime rangeEndLocal,
        TimeZoneInfo timeZone
    )
    {
        DateTime? untilLocal = rule.Until.HasValue
            ? rule.UntilIsUtc
                ? ICalendarTimeZoneHelper.ToLocalWallTime(
                    new DateTimeOffset(DateTime.SpecifyKind(rule.Until.Value, DateTimeKind.Utc)),
                    timeZone
                )
                : rule.Until.Value
            : null;
        var limitEnd =
            untilLocal.HasValue && untilLocal.Value < rangeEndLocal
                ? untilLocal.Value
                : rangeEndLocal;
        var maxCount = rule.Count.HasValue
            ? Math.Min(rule.Count.Value, MaxEvaluatedOccurrences)
            : MaxEvaluatedOccurrences;

        return rule.Frequency switch
        {
            RecurrenceFrequency.Daily => ExpandDaily(dtStart, rule, limitEnd, maxCount),
            RecurrenceFrequency.Weekly => ExpandWeekly(dtStart, rule, limitEnd, maxCount),
            RecurrenceFrequency.Monthly => ExpandMonthly(dtStart, rule, limitEnd, maxCount),
            RecurrenceFrequency.Yearly => ExpandYearly(dtStart, rule, limitEnd, maxCount),
            _ => [],
        };
    }

    private static List<DateTime> ExpandDaily(
        DateTime dtStart,
        ICalRecurrenceRule rule,
        DateTime limitEnd,
        int maxCount
    )
    {
        var allowedDays = rule.ByDays.Select(static d => d.DayOfWeek).ToHashSet();
        var list = new List<DateTime>();
        var cursor = dtStart;

        while (cursor <= limitEnd && list.Count < maxCount)
        {
            if (allowedDays.Count == 0 || allowedDays.Contains(cursor.DayOfWeek))
            {
                list.Add(cursor);
            }

            cursor = cursor.AddDays(rule.Interval);
        }

        return list;
    }

    private static List<DateTime> ExpandWeekly(
        DateTime dtStart,
        ICalRecurrenceRule rule,
        DateTime limitEnd,
        int maxCount
    )
    {
        var targetDays =
            rule.ByDays.Count > 0
                ? rule.ByDays.Select(static d => d.DayOfWeek).Distinct().ToHashSet()
                : [dtStart.DayOfWeek];
        var list = new List<DateTime>();
        var weekStartDate = AlignToWeekStart(dtStart.Date, rule.WeekStart);

        while (weekStartDate <= limitEnd.Date && list.Count < maxCount)
        {
            for (var offset = 0; offset < 7 && list.Count < maxCount; offset++)
            {
                var candidateDate = weekStartDate.AddDays(offset);
                if (!targetDays.Contains(candidateDate.DayOfWeek))
                {
                    continue;
                }

                var candidate = candidateDate.Add(dtStart.TimeOfDay);
                if (candidate >= dtStart && candidate <= limitEnd)
                {
                    list.Add(candidate);
                }
            }

            weekStartDate = weekStartDate.AddDays(7 * rule.Interval);
        }

        return list;
    }

    private static List<DateTime> ExpandMonthly(
        DateTime dtStart,
        ICalRecurrenceRule rule,
        DateTime limitEnd,
        int maxCount
    )
    {
        var list = new List<DateTime>();
        var year = dtStart.Year;
        var month = dtStart.Month;

        while (new DateTime(year, month, 1) <= limitEnd.Date && list.Count < maxCount)
        {
            var monthDates = GetMonthlyCandidateDates(year, month, dtStart.Day, rule);
            foreach (var date in ApplyBySetPos(monthDates, rule.BySetPos))
            {
                var candidate = date.Add(dtStart.TimeOfDay);
                if (candidate >= dtStart && candidate <= limitEnd && list.Count < maxCount)
                {
                    list.Add(candidate);
                }
            }

            (year, month) = AddMonths(year, month, rule.Interval);
        }

        return list;
    }

    private static List<DateTime> ExpandYearly(
        DateTime dtStart,
        ICalRecurrenceRule rule,
        DateTime limitEnd,
        int maxCount
    )
    {
        var list = new List<DateTime>();
        var months =
            rule.ByMonths.Count > 0 ? rule.ByMonths.Order().Distinct().ToArray() : [dtStart.Month];
        var year = dtStart.Year;

        while (year <= limitEnd.Year && list.Count < maxCount)
        {
            var yearDates = new List<DateTime>();
            foreach (var month in months)
            {
                yearDates.AddRange(GetMonthlyCandidateDates(year, month, dtStart.Day, rule));
            }

            foreach (var date in ApplyBySetPos(yearDates, rule.BySetPos))
            {
                var candidate = date.Add(dtStart.TimeOfDay);
                if (candidate >= dtStart && candidate <= limitEnd && list.Count < maxCount)
                {
                    list.Add(candidate);
                }
            }

            year += rule.Interval;
        }

        return list;
    }

    private static List<DateTime> GetMonthlyCandidateDates(
        int year,
        int month,
        int defaultDay,
        ICalRecurrenceRule rule
    )
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (rule.ByDays.Count > 0)
        {
            return ResolveByDayInMonth(year, month, daysInMonth, rule.ByDays);
        }

        var targetDays = rule.ByMonthDays.Count > 0 ? rule.ByMonthDays : [defaultDay];
        var dates = new SortedSet<DateTime>();
        foreach (var rawDay in targetDays)
        {
            var resolved = rawDay > 0 ? rawDay : daysInMonth + 1 + rawDay;
            if (resolved >= 1 && resolved <= daysInMonth)
            {
                _ = dates.Add(new DateTime(year, month, resolved));
            }
        }

        return [.. dates];
    }

    private static List<DateTime> ResolveByDayInMonth(
        int year,
        int month,
        int daysInMonth,
        IReadOnlyList<ByDayRule> byDays
    )
    {
        var matched = new SortedSet<DateTime>();
        foreach (var rule in byDays)
        {
            var allOfWeekday = new List<DateTime>();
            for (var d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(year, month, d);
                if (dt.DayOfWeek == rule.DayOfWeek)
                {
                    allOfWeekday.Add(dt);
                }
            }

            if (!rule.Ordinal.HasValue)
            {
                foreach (var dt in allOfWeekday)
                {
                    _ = matched.Add(dt);
                }
            }
            else
            {
                var idx =
                    rule.Ordinal.Value > 0
                        ? rule.Ordinal.Value - 1
                        : allOfWeekday.Count + rule.Ordinal.Value;
                if (idx >= 0 && idx < allOfWeekday.Count)
                {
                    _ = matched.Add(allOfWeekday[idx]);
                }
            }
        }

        return [.. matched];
    }

    private static List<DateTime> ApplyBySetPos(
        List<DateTime> sortedDates,
        IReadOnlyList<int> bySetPos
    )
    {
        if (bySetPos.Count == 0 || sortedDates.Count == 0)
        {
            return sortedDates;
        }

        var selected = new SortedSet<DateTime>();
        foreach (var pos in bySetPos)
        {
            var idx = pos > 0 ? pos - 1 : sortedDates.Count + pos;
            if (idx >= 0 && idx < sortedDates.Count)
            {
                _ = selected.Add(sortedDates[idx]);
            }
        }

        return [.. selected];
    }

    private static DateTime AlignToWeekStart(DateTime date, DayOfWeek weekStart)
    {
        var diff = (7 + (date.DayOfWeek - weekStart)) % 7;
        return date.AddDays(-diff);
    }

    private static (int Year, int Month) AddMonths(int year, int month, int deltaMonths)
    {
        var zeroBased = (year * 12) + (month - 1) + deltaMonths;
        return (zeroBased / 12, (zeroBased % 12) + 1);
    }

    private static HashSet<DateTime> BuildExDateSet(IReadOnlyList<DateTime> exDates)
    {
        var set = new HashSet<DateTime>();
        foreach (var d in exDates)
        {
            _ = set.Add(d);
        }

        return set;
    }

    private static bool IsExcludedByExDate(
        DateTime localStart,
        HashSet<DateTime> exDates,
        bool isAllDay
    )
    {
        return exDates.Count != 0
            && (exDates.Contains(localStart) || (isAllDay && exDates.Contains(localStart.Date)));
    }

    private static bool IsExcludedByCategory(
        DateTimeOffset startUtc,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? exclusions
    )
    {
        if (exclusions is null || exclusions.Count == 0)
        {
            return false;
        }

        foreach (var (start, end) in exclusions)
        {
            if (startUtc >= start && startUtc <= end)
            {
                return true;
            }
        }

        return false;
    }
}
