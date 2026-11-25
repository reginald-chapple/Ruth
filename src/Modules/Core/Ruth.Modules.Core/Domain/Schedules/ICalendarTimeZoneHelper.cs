namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>DST-safe timezone conversion helpers for RFC 5545 floating and zoned wall-clock times.</summary>
internal static class ICalendarTimeZoneHelper
{
    public static TimeZoneInfo ResolveTimeZone(
        string? preferredTimeZoneId,
        string? fallbackTimeZoneId = null
    )
    {
        return TryGetTimeZone(preferredTimeZoneId, out var primary) ? primary
            : TryGetTimeZone(fallbackTimeZoneId, out var fallback) ? fallback
            : TimeZoneInfo.Utc;
    }

    public static DateTimeOffset ToUtcOffset(
        DateTime localWallTime,
        TimeZoneInfo timeZone,
        bool isAlreadyUtc
    )
    {
        if (isAlreadyUtc)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(localWallTime, DateTimeKind.Utc));
        }

        var unspecified = DateTime.SpecifyKind(localWallTime, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(unspecified))
        {
            var adjusted = AdvancePastSpringForwardGap(unspecified, timeZone);
            var offset = timeZone.GetUtcOffset(adjusted);
            return new DateTimeOffset(adjusted, offset).ToUniversalTime();
        }

        if (timeZone.IsAmbiguousTime(unspecified))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(unspecified);
            var firstOccurrenceOffset = offsets.Max();
            return new DateTimeOffset(unspecified, firstOccurrenceOffset).ToUniversalTime();
        }

        var standardOffset = timeZone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, standardOffset).ToUniversalTime();
    }

    public static DateTime ToLocalWallTime(DateTimeOffset utcInstant, TimeZoneInfo timeZone)
    {
        var converted = TimeZoneInfo.ConvertTime(utcInstant, timeZone);
        return DateTime.SpecifyKind(converted.DateTime, DateTimeKind.Unspecified);
    }

    private static DateTime AdvancePastSpringForwardGap(
        DateTime invalidWallTime,
        TimeZoneInfo timeZone
    )
    {
        var probe = invalidWallTime;
        for (var minute = 1; minute <= 180; minute++)
        {
            probe = invalidWallTime.AddMinutes(minute);
            if (!timeZone.IsInvalidTime(probe))
            {
                var beforeOffset = timeZone.GetUtcOffset(invalidWallTime.AddHours(-2));
                var afterOffset = timeZone.GetUtcOffset(probe);
                var delta = afterOffset - beforeOffset;
                return delta > TimeSpan.Zero ? invalidWallTime.Add(delta) : probe;
            }
        }

        return probe;
    }

    private static bool TryGetTimeZone(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        if (
            !string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var found)
        )
        {
            timeZone = found;
            return true;
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }
}
