using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Parses and generates RFC 5545 VCALENDAR/VEVENT content.</summary>
internal static partial class ICalendarParser
{
    public static Result<ICalEventSpec> Parse(string iCalendarContent)
    {
        if (string.IsNullOrWhiteSpace(iCalendarContent))
        {
            return CoreScheduleErrors.ScheduleICalendarInvalid;
        }

        var lines = UnfoldLines(iCalendarContent);
        var state = new ParseState();
        foreach (var line in lines)
        {
            var parseLine = ProcessLine(line, state);
            if (parseLine.IsFailure)
            {
                return Result.Failure<ICalEventSpec>(parseLine.Errors);
            }
        }

        if (!state.DtStart.HasValue)
        {
            return CoreScheduleErrors.ScheduleICalendarInvalid;
        }

        var duration = ResolveDuration(state);
        return Result.Success(
            new ICalEventSpec(
                state.DtStart.Value,
                state.DtStartIsUtc,
                state.IsAllDay,
                duration,
                state.TimeZoneId,
                state.RRule,
                state.RDates,
                state.ExDates
            )
        );
    }

    public static string BuildSimpleICalendar(
        DateTime startLocal,
        TimeSpan duration,
        string? rrule = null,
        string? timeZoneId = null,
        IReadOnlyList<DateTime>? exDates = null
    )
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("BEGIN:VCALENDAR");
        _ = sb.AppendLine("VERSION:2.0");
        _ = sb.AppendLine("PRODID:-//Ruth RMS//Schedule Engine//EN");
        _ = sb.AppendLine("BEGIN:VEVENT");
        var dtStartKey = string.IsNullOrWhiteSpace(timeZoneId)
            ? "DTSTART"
            : $"DTSTART;TZID={timeZoneId.Trim()}";
        _ = sb.AppendLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{dtStartKey}:{startLocal:yyyyMMdd'T'HHmmss}"
            )
        );
        var endLocal = startLocal.Add(duration < TimeSpan.Zero ? TimeSpan.Zero : duration);
        var dtEndKey = string.IsNullOrWhiteSpace(timeZoneId)
            ? "DTEND"
            : $"DTEND;TZID={timeZoneId.Trim()}";
        _ = sb.AppendLine(
            string.Create(CultureInfo.InvariantCulture, $"{dtEndKey}:{endLocal:yyyyMMdd'T'HHmmss}")
        );
        if (!string.IsNullOrWhiteSpace(rrule))
        {
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"RRULE:{rrule.Trim()}");
        }

        if (exDates is { Count: > 0 })
        {
            var formatted = string.Join(
                ',',
                exDates.Select(static d =>
                    d.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture)
                )
            );
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"EXDATE:{formatted}");
        }

        _ = sb.AppendLine("END:VEVENT");
        _ = sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    internal static bool TryParseDateTimeToken(
        string token,
        out DateTime value,
        out bool isUtc,
        out bool isAllDay
    )
    {
        var trimmed = token.Trim();
        var slashIdx = trimmed.IndexOf('/', StringComparison.Ordinal);
        if (slashIdx > 0)
        {
            trimmed = trimmed[..slashIdx];
        }

        isUtc = trimmed.EndsWith('Z');
        if (isUtc)
        {
            trimmed = trimmed[..^1];
        }

        if (
            trimmed.Length == 8
            && DateTime.TryParseExact(
                trimmed,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value
            )
        )
        {
            isAllDay = true;
            return true;
        }

        isAllDay = false;
        return DateTime.TryParseExact(
            trimmed,
            "yyyyMMdd'T'HHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value
        );
    }

    private static List<string> UnfoldLines(string content)
    {
        var normalized = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var rawLines = normalized.Split('\n');
        var unfolded = new List<string>(rawLines.Length);
        foreach (var line in rawLines)
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && unfolded.Count > 0)
            {
                unfolded[^1] += line[1..];
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                unfolded.Add(line.TrimEnd());
            }
        }

        return unfolded;
    }

    private static Result ProcessLine(string line, ParseState state)
    {
        if (string.Equals(line, "BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
        {
            state.InEvent = true;
            return Result.Success();
        }

        if (string.Equals(line, "END:VEVENT", StringComparison.OrdinalIgnoreCase))
        {
            state.InEvent = false;
            return Result.Success();
        }

        if (!state.InEvent)
        {
            return Result.Success();
        }

        var colon = line.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0)
        {
            return Result.Success();
        }

        var header = line[..colon];
        var val = line[(colon + 1)..].Trim();
        var (propName, tzId) = ParsePropertyHeader(header);
        return ApplyProperty(propName, tzId, val, state);
    }

    private static (string Name, string? TzId) ParsePropertyHeader(string header)
    {
        var parts = header.Split(';');
        var name = parts[0].Trim().ToUpperInvariant();
        string? tzId = null;
        for (var i = 1; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("TZID=", StringComparison.OrdinalIgnoreCase))
            {
                tzId = parts[i]["TZID=".Length..].Trim().Trim('"');
            }
        }

        return (name, tzId);
    }

    private static Result ApplyProperty(string propName, string? tzId, string val, ParseState state)
    {
        switch (propName)
        {
            case "DTSTART":
                return ParseDtStart(val, tzId, state);
            case "DTEND":
                if (TryParseDateTimeToken(val, out var dtEnd, out _, out _))
                {
                    state.DtEnd = dtEnd;
                }

                return Result.Success();
            case "DURATION":
                if (TryParseIsoDuration(val, out var duration))
                {
                    state.ExplicitDuration = duration;
                }

                return Result.Success();
            case "RRULE":
                var rrule = ICalendarRRuleParser.Parse(val);
                if (rrule.IsFailure)
                {
                    return Result.Failure(rrule.Errors);
                }

                state.RRule = rrule.Value;
                return Result.Success();
            case "RDATE":
                AppendDateList(val, state.RDates);
                return Result.Success();
            case "EXDATE":
                AppendDateList(val, state.ExDates);
                return Result.Success();
            default:
                return Result.Success();
        }
    }

    private static Result ParseDtStart(string val, string? tzId, ParseState state)
    {
        if (!TryParseDateTimeToken(val, out var dt, out var isUtc, out var isAllDay))
        {
            return CoreScheduleErrors.ScheduleICalendarInvalid;
        }

        state.DtStart = dt;
        state.DtStartIsUtc = isUtc;
        state.IsAllDay = isAllDay;
        state.TimeZoneId ??= tzId;
        return Result.Success();
    }

    private static void AppendDateList(string value, List<DateTime> target)
    {
        foreach (
            var token in value.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            if (TryParseDateTimeToken(token, out var parsed, out _, out _))
            {
                target.Add(parsed);
            }
        }
    }

    private static TimeSpan ResolveDuration(ParseState state)
    {
        return state.ExplicitDuration.HasValue
            ? state.ExplicitDuration.Value < TimeSpan.Zero
                ? TimeSpan.Zero
                : state.ExplicitDuration.Value
            : state.DtEnd.HasValue
            && state.DtStart.HasValue
            && state.DtEnd.Value >= state.DtStart.Value
                ? state.DtEnd.Value - state.DtStart.Value
                : state.IsAllDay
                    ? TimeSpan.FromDays(1)
                    : TimeSpan.Zero;
    }

    private static bool TryParseIsoDuration(string value, out TimeSpan duration)
    {
        var match = IsoDurationRegex.Match(value.Trim().ToUpperInvariant());
        if (!match.Success)
        {
            duration = TimeSpan.Zero;
            return false;
        }

        var weeks = ParseGroupInt(match, "w");
        var days = ParseGroupInt(match, "d");
        var hours = ParseGroupInt(match, "h");
        var minutes = ParseGroupInt(match, "m");
        var seconds = ParseGroupInt(match, "s");
        duration = new TimeSpan((weeks * 7) + days, hours, minutes, seconds);
        return true;
    }

    private static int ParseGroupInt(Match match, string groupName)
    {
        return
            match.Groups[groupName].Success
            && int.TryParse(
                match.Groups[groupName].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var n
            )
            ? n
            : 0;
    }

    [GeneratedRegex(
        @"^P(?:(?<w>\d+)W)?(?:(?<d>\d+)D)?(?:T(?:(?<h>\d+)H)?(?:(?<m>\d+)M)?(?:(?<s>\d+)S)?)?$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex IsoDurationRegex { get; }

    private sealed class ParseState
    {
        public bool InEvent { get; set; }
        public DateTime? DtStart { get; set; }
        public bool DtStartIsUtc { get; set; }
        public bool IsAllDay { get; set; }
        public DateTime? DtEnd { get; set; }
        public TimeSpan? ExplicitDuration { get; set; }
        public string? TimeZoneId { get; set; }
        public ICalRecurrenceRule? RRule { get; set; }
        public List<DateTime> RDates { get; } = [];
        public List<DateTime> ExDates { get; } = [];
    }
}
