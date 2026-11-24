namespace Ruth.Persistence.Scheduling;

internal static class CronOccurrenceCalculator
{
    public static DateTimeOffset? GetNextOccurrence(
        in CronScheduleFields fields,
        DateTimeOffset afterUtc,
        TimeZoneInfo timeZone
    )
    {
        var normalizedAfter = afterUtc.ToUniversalTime();
        var afterLocal = TimeZoneInfo.ConvertTime(normalizedAfter, timeZone);
        var isAmbiguousStart = timeZone.IsAmbiguousTime(afterLocal.DateTime);
        var cursor = isAmbiguousStart
            ? new DateTime(
                afterLocal.Year,
                afterLocal.Month,
                afterLocal.Day,
                afterLocal.Hour,
                0,
                0,
                DateTimeKind.Unspecified
            )
            : StripSubSeconds(afterLocal.DateTime).AddSeconds(1);

        var maxYear = cursor.Year + 8;
        while (cursor.Year <= maxYear)
        {
            var date = DateOnly.FromDateTime(cursor);
            if (!MatchesDate(in fields, date))
            {
                cursor = cursor.Date.AddDays(1);
                continue;
            }

            var candidate = FindNextMatchingUtcOnDate(
                in fields,
                date,
                TimeOnly.FromDateTime(cursor),
                normalizedAfter,
                timeZone
            );
            if (candidate.HasValue)
            {
                return candidate;
            }

            cursor = cursor.Date.AddDays(1);
        }

        return null;
    }

    private static DateTime StripSubSeconds(DateTime value)
    {
        return new DateTime(
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            value.Second,
            DateTimeKind.Unspecified
        );
    }

    private static bool MatchesDate(in CronScheduleFields fields, DateOnly date)
    {
        if (!fields.Months.Contains(date.Month))
        {
            return false;
        }

        var domMatch = fields.DaysOfMonth.Contains(date.Day);
        var dowMatch = fields.DaysOfWeek.Contains((int)date.DayOfWeek);

        return !fields.DaysOfMonth.IsWildcard && !fields.DaysOfWeek.IsWildcard
            ? domMatch || dowMatch
            : domMatch && dowMatch;
    }

    private static DateTimeOffset? FindNextMatchingUtcOnDate(
        in CronScheduleFields fields,
        DateOnly date,
        TimeOnly startTime,
        DateTimeOffset afterUtc,
        TimeZoneInfo timeZone
    )
    {
        var hour = fields.Hours.NextOrEqual(startTime.Hour, 23);
        while (hour.HasValue)
        {
            var minStart = hour.Value == startTime.Hour ? startTime.Minute : 0;
            var secondStart = hour.Value == startTime.Hour ? startTime.Second : 0;
            var match = FindInHour(
                in fields,
                date,
                hour.Value,
                minStart,
                secondStart,
                afterUtc,
                timeZone
            );
            if (match.HasValue)
            {
                return match;
            }

            hour = fields.Hours.NextOrEqual(hour.Value + 1, 23);
        }

        return null;
    }

    private static DateTimeOffset? FindInHour(
        in CronScheduleFields fields,
        DateOnly date,
        int hour,
        int minStart,
        int secondStart,
        DateTimeOffset afterUtc,
        TimeZoneInfo timeZone
    )
    {
        var hourStart = date.ToDateTime(new TimeOnly(hour, 0, 0));
        var hourEnd = date.ToDateTime(new TimeOnly(hour, 59, 59));
        var hasAmbiguous = timeZone.IsAmbiguousTime(hourStart) || timeZone.IsAmbiguousTime(hourEnd);

        DateTimeOffset? earliest = null;
        var minute = fields.Minutes.NextOrEqual(minStart, 59);
        while (minute.HasValue)
        {
            var secBegin = minute.Value == minStart ? secondStart : 0;
            var candidate = FindInMinute(
                fields.Seconds,
                date,
                hour,
                minute.Value,
                secBegin,
                afterUtc,
                timeZone,
                hasAmbiguous
            );
            if (candidate.HasValue)
            {
                if (!hasAmbiguous)
                {
                    return candidate;
                }

                if (!earliest.HasValue || candidate.Value < earliest.Value)
                {
                    earliest = candidate;
                }
            }

            minute = fields.Minutes.NextOrEqual(minute.Value + 1, 59);
        }

        return earliest;
    }

    private static DateTimeOffset? FindInMinute(
        CronField secondsField,
        DateOnly date,
        int hour,
        int minute,
        int secondStart,
        DateTimeOffset afterUtc,
        TimeZoneInfo timeZone,
        bool hasAmbiguous
    )
    {
        DateTimeOffset? earliest = null;
        var second = secondsField.NextOrEqual(secondStart, 59);
        while (second.HasValue)
        {
            var local = date.ToDateTime(new TimeOnly(hour, minute, second.Value));
            var resolved = ResolveLocalToUtc(local, afterUtc, timeZone);
            if (resolved.HasValue)
            {
                if (!hasAmbiguous)
                {
                    return resolved;
                }

                if (!earliest.HasValue || resolved.Value < earliest.Value)
                {
                    earliest = resolved;
                }
            }

            second = secondsField.NextOrEqual(second.Value + 1, 59);
        }

        return earliest;
    }

    private static DateTimeOffset? ResolveLocalToUtc(
        DateTime localUnspecified,
        DateTimeOffset afterUtc,
        TimeZoneInfo timeZone
    )
    {
        if (timeZone.IsInvalidTime(localUnspecified))
        {
            return null;
        }

        if (timeZone.IsAmbiguousTime(localUnspecified))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(localUnspecified);
            Array.Sort(offsets, static (a, b) => b.CompareTo(a));
            foreach (var offset in offsets)
            {
                var candidate = new DateTimeOffset(localUnspecified, offset).ToUniversalTime();
                if (candidate > afterUtc)
                {
                    return candidate;
                }
            }

            return null;
        }

        var utcCandidate = new DateTimeOffset(
            localUnspecified,
            timeZone.GetUtcOffset(localUnspecified)
        ).ToUniversalTime();
        return utcCandidate > afterUtc ? utcCandidate : null;
    }
}
