using System.Globalization;
using System.Text.RegularExpressions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Schedules;

internal static partial class ICalendarRRuleParser
{
    public static Result<ICalRecurrenceRule> Parse(string rawRule)
    {
        var parts = ParseKeyValuePairs(rawRule);
        if (
            !parts.TryGetValue("FREQ", out var freqText)
            || !TryParseFrequency(freqText, out var frequency)
        )
        {
            return CoreScheduleErrors.ScheduleICalendarInvalid;
        }

        var intervalResult = ParsePositiveInt(parts, "INTERVAL", defaultValue: 1);
        var countResult = ParseOptionalPositiveInt(parts, "COUNT");
        var untilResult = ParseOptionalUntil(parts);
        var combined = Result.Combine(intervalResult, countResult, untilResult);
        if (combined.IsFailure)
        {
            return Result.Failure<ICalRecurrenceRule>(combined.Errors);
        }

        var byDays = parts.TryGetValue("BYDAY", out var byDayText) ? ParseByDays(byDayText) : [];
        var byMonthDays = parts.TryGetValue("BYMONTHDAY", out var bmdText)
            ? ParseIntList(bmdText, -31, 31)
            : [];
        var byMonths = parts.TryGetValue("BYMONTH", out var bmText)
            ? ParseIntList(bmText, 1, 12)
            : [];
        var bySetPos = parts.TryGetValue("BYSETPOS", out var bspText)
            ? ParseIntList(bspText, -366, 366)
            : [];
        var weekStart =
            parts.TryGetValue("WKST", out var wkstText) && TryParseDayToken(wkstText, out var ws)
                ? ws
                : DayOfWeek.Monday;

        return Result.Success(
            new ICalRecurrenceRule(
                frequency,
                intervalResult.Value,
                countResult.Value,
                untilResult.Value.Until,
                untilResult.Value.IsUtc,
                byDays,
                byMonthDays,
                byMonths,
                bySetPos,
                weekStart
            )
        );
    }

    private static Result<int> ParsePositiveInt(
        Dictionary<string, string> parts,
        string key,
        int defaultValue
    )
    {
        return !parts.TryGetValue(key, out var raw) ? Result.Success(defaultValue)
            : int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed >= 1
                ? Result.Success(parsed)
            : CoreScheduleErrors.ScheduleICalendarInvalid;
    }

    private static Result<int?> ParseOptionalPositiveInt(
        Dictionary<string, string> parts,
        string key
    )
    {
        return !parts.TryGetValue(key, out var raw) ? Result.Success<int?>(value: null)
            : int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed >= 1
                ? Result.Success<int?>(parsed)
            : CoreScheduleErrors.ScheduleICalendarInvalid;
    }

    private static Result<(DateTime? Until, bool IsUtc)> ParseOptionalUntil(
        Dictionary<string, string> parts
    )
    {
        if (!parts.TryGetValue("UNTIL", out var raw))
        {
            return Result.Success<(DateTime?, bool)>((null, false));
        }

        if (
            !ICalendarParser.TryParseDateTimeToken(
                raw,
                out var parsedUntil,
                out var isUtc,
                out var isAllDay
            )
        )
        {
            return CoreScheduleErrors.ScheduleICalendarInvalid;
        }

        var normalized = isAllDay ? parsedUntil.Date.AddDays(1).AddSeconds(-1) : parsedUntil;
        return Result.Success<(DateTime?, bool)>((normalized, isUtc));
    }

    private static Dictionary<string, string> ParseKeyValuePairs(string rawRule)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (
            var token in rawRule.Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            var eq = token.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0 && eq < token.Length - 1)
            {
                result[token[..eq].Trim()] = token[(eq + 1)..].Trim();
            }
        }

        return result;
    }

    private static bool TryParseFrequency(string text, out RecurrenceFrequency frequency)
    {
        frequency = text.ToUpperInvariant() switch
        {
            "DAILY" => RecurrenceFrequency.Daily,
            "WEEKLY" => RecurrenceFrequency.Weekly,
            "MONTHLY" => RecurrenceFrequency.Monthly,
            "YEARLY" => RecurrenceFrequency.Yearly,
            _ => (RecurrenceFrequency)(-1),
        };
        return Enum.IsDefined(frequency);
    }

    private static List<ByDayRule> ParseByDays(string text)
    {
        var list = new List<ByDayRule>();
        foreach (
            var token in text.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            var match = ByDayRegex.Match(token.ToUpperInvariant());
            if (!match.Success || !TryParseDayToken(match.Groups["day"].Value, out var dow))
            {
                continue;
            }

            int? ordinal = null;
            if (
                match.Groups["ord"].Success
                && int.TryParse(
                    match.Groups["ord"].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedOrd
                )
                && parsedOrd != 0
            )
            {
                ordinal = parsedOrd;
            }

            list.Add(new ByDayRule(dow, ordinal));
        }

        return list;
    }

    private static List<int> ParseIntList(string text, int min, int max)
    {
        var list = new List<int>();
        foreach (
            var token in text.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            if (
                int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)
                && val >= min
                && val <= max
                && val != 0
            )
            {
                list.Add(val);
            }
        }

        return list;
    }

    internal static bool TryParseDayToken(string token, out DayOfWeek day)
    {
        day = token.Trim().ToUpperInvariant() switch
        {
            "SU" => DayOfWeek.Sunday,
            "MO" => DayOfWeek.Monday,
            "TU" => DayOfWeek.Tuesday,
            "WE" => DayOfWeek.Wednesday,
            "TH" => DayOfWeek.Thursday,
            "FR" => DayOfWeek.Friday,
            "SA" => DayOfWeek.Saturday,
            _ => (DayOfWeek)(-1),
        };
        return Enum.IsDefined(day);
    }

    [GeneratedRegex(
        @"^(?<ord>[+-]?\d+)?(?<day>SU|MO|TU|WE|TH|FR|SA)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex ByDayRegex { get; }
}
