using System.Globalization;

namespace Ruth.Persistence.Scheduling;

internal static class CronFieldParser
{
    private static readonly Dictionary<string, int> _monthAliases = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["JAN"] = 1,
        ["FEB"] = 2,
        ["MAR"] = 3,
        ["APR"] = 4,
        ["MAY"] = 5,
        ["JUN"] = 6,
        ["JUL"] = 7,
        ["AUG"] = 8,
        ["SEP"] = 9,
        ["OCT"] = 10,
        ["NOV"] = 11,
        ["DEC"] = 12,
    };

    private static readonly Dictionary<string, int> _dayOfWeekAliases = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["SUN"] = 0,
        ["MON"] = 1,
        ["TUE"] = 2,
        ["WED"] = 3,
        ["THU"] = 4,
        ["FRI"] = 5,
        ["SAT"] = 6,
    };

    public static bool TryParse(
        string token,
        int min,
        int max,
        CronFieldKind kind,
        out CronField field
    )
    {
        field = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmed = token.Trim();
        if (IsWildcardToken(trimmed, kind))
        {
            field = new CronField(CreateRangeMask(min, max, 1, kind), IsWildcard: true);
            return true;
        }

        ulong combinedMask = 0;
        foreach (var part in trimmed.Split(','))
        {
            if (!TryParseSegment(part, min, max, kind, out var segmentMask))
            {
                return false;
            }

            combinedMask |= segmentMask;
        }

        if (combinedMask == 0)
        {
            return false;
        }

        field = new CronField(combinedMask, IsWildcard: false);
        return true;
    }

    private static bool IsWildcardToken(string token, CronFieldKind kind)
    {
        return string.Equals(token, "*", StringComparison.Ordinal)
            || (
                string.Equals(token, "?", StringComparison.Ordinal)
                && kind is CronFieldKind.DayOfMonth or CronFieldKind.DayOfWeek
            );
    }

    private static bool TryParseSegment(
        string segment,
        int min,
        int max,
        CronFieldKind kind,
        out ulong mask
    )
    {
        mask = 0;
        if (string.IsNullOrWhiteSpace(segment))
        {
            return false;
        }

        var slashParts = segment.Split('/');
        if (slashParts.Length > 2)
        {
            return false;
        }

        var step = 1;
        return (slashParts.Length != 2 || TryParseStep(slashParts[1], out step))
            && TryParseRange(slashParts[0], min, max, step, kind, out mask);
    }

    private static bool TryParseStep(string token, out int step)
    {
        return int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out step)
            && step > 0;
    }

    private static bool TryParseRange(
        string rangePart,
        int min,
        int max,
        int step,
        CronFieldKind kind,
        out ulong mask
    )
    {
        mask = 0;
        if (IsWildcardToken(rangePart, kind))
        {
            mask = CreateRangeMask(min, max, step, kind);
            return true;
        }

        var dashParts = rangePart.Split('-');
        if (dashParts.Length == 1)
        {
            return TryParseSingleRange(dashParts[0], min, max, step, kind, out mask);
        }

        if (dashParts.Length != 2)
        {
            return false;
        }

        if (
            !TryParseValue(dashParts[0], min, max, kind, out var start)
            || !TryParseValue(dashParts[1], min, max, kind, out var end)
            || start > end
        )
        {
            return false;
        }

        mask = CreateRangeMask(start, end, step, kind);
        return true;
    }

    private static bool TryParseSingleRange(
        string valueToken,
        int min,
        int max,
        int step,
        CronFieldKind kind,
        out ulong mask
    )
    {
        mask = 0;
        if (!TryParseValue(valueToken, min, max, kind, out var start))
        {
            return false;
        }

        if (step > 1)
        {
            if (kind == CronFieldKind.DayOfWeek && start == 7)
            {
                start = 0;
            }

            mask = CreateRangeMask(start, max, step, kind);
            return true;
        }

        mask = CreateRangeMask(start, start, step, kind);
        return true;
    }

    private static bool TryParseValue(
        string token,
        int min,
        int max,
        CronFieldKind kind,
        out int value
    )
    {
        if (TryResolveAlias(token, kind, out value))
        {
            return value >= min && value <= max;
        }

        if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        var upperBound = kind == CronFieldKind.DayOfWeek ? 7 : max;
        return value >= min && value <= upperBound;
    }

    private static bool TryResolveAlias(string token, CronFieldKind kind, out int value)
    {
        value = 0;
        return kind switch
        {
            CronFieldKind.Month => _monthAliases.TryGetValue(token, out value),
            CronFieldKind.DayOfWeek => _dayOfWeekAliases.TryGetValue(token, out value),
            CronFieldKind.Second
            or CronFieldKind.Minute
            or CronFieldKind.Hour
            or CronFieldKind.DayOfMonth => false,
            _ => false,
        };
    }

    private static ulong CreateRangeMask(int start, int end, int step, CronFieldKind kind)
    {
        ulong mask = 0;
        for (var value = start; value <= end; value += step)
        {
            var bit = kind == CronFieldKind.DayOfWeek && value == 7 ? 0 : value;
            mask |= 1UL << bit;
        }

        return mask;
    }
}
