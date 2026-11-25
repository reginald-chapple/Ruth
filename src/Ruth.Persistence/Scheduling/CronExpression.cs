using System.Diagnostics.CodeAnalysis;

namespace Ruth.Persistence.Scheduling;

/// <summary>
/// Pure-C# 5-field and 6-field cron expression parser and DST-aware occurrence calculator.
/// Supports standard macros (@hourly, @daily, @never, etc.).
/// </summary>
public sealed class CronExpression
{
    /// <summary>Sentinel cron expression representing an on-demand-only job that never runs on a schedule.</summary>
    public const string NeverExpression = "@never";

    /// <summary>Alternate manual-only macro treated identically to <see cref="NeverExpression" />.</summary>
    public const string ManualExpression = "@manual";

    private static readonly Dictionary<string, string> _macros = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["@yearly"] = "0 0 1 1 *",
        ["@annually"] = "0 0 1 1 *",
        ["@monthly"] = "0 0 1 * *",
        ["@weekly"] = "0 0 * * 0",
        ["@daily"] = "0 0 * * *",
        ["@midnight"] = "0 0 * * *",
        ["@hourly"] = "0 * * * *",
    };

    private readonly CronScheduleFields _fields;

    private CronExpression(string rawExpression, bool isNever, CronScheduleFields fields)
    {
        Expression = rawExpression;
        IsNever = isNever;
        _fields = fields;
    }

    /// <summary>Gets the original normalized cron expression text.</summary>
    public string Expression { get; }

    /// <summary>Gets a value indicating whether this expression never produces scheduled occurrences.</summary>
    public bool IsNever { get; }

    /// <summary>Returns true when <paramref name="expression" /> is a valid cron expression.</summary>
    public static bool IsValid(string? expression)
    {
        return TryParse(expression, out _);
    }

    /// <summary>Parses a cron expression or throws <see cref="FormatException" /> if invalid.</summary>
    public static CronExpression Parse(string expression)
    {
        return TryParse(expression, out var parsed)
            ? parsed
            : throw new FormatException($"Invalid cron expression '{expression}'.");
    }

    /// <summary>Attempts to parse a 5-field, 6-field, or macro cron expression.</summary>
    public static bool TryParse(
        string? expression,
        [NotNullWhen(true)] out CronExpression? cronExpression
    )
    {
        cronExpression = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var trimmed = expression.Trim();
        if (IsNeverSentinel(trimmed))
        {
            cronExpression = new CronExpression(trimmed, isNever: true, default);
            return true;
        }

        var expanded = _macros.TryGetValue(trimmed, out var macro) ? macro : trimmed;
        var parts = expanded.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length is 5 or 6 && TryBuildFromFields(trimmed, parts, out cronExpression);
    }

    /// <summary>
    /// Computes the next UTC occurrence strictly after <paramref name="afterUtc" /> evaluated in <paramref name="timeZone" />.
    /// </summary>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset afterUtc, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        return IsNever
            ? null
            : CronOccurrenceCalculator.GetNextOccurrence(in _fields, afterUtc, timeZone);
    }

    private static bool TryBuildFromFields(
        string original,
        string[] parts,
        [NotNullWhen(true)] out CronExpression? cronExpression
    )
    {
        cronExpression = null;
        var offset = parts.Length == 6 ? 1 : 0;
        if (!TryParseSecondsField(parts, offset, out var seconds))
        {
            return false;
        }

        if (!TryParseCalendarAndTimeFields(parts, offset, seconds, out var fields))
        {
            return false;
        }

        cronExpression = new CronExpression(original, isNever: false, fields);
        return true;
    }

    private static bool TryParseSecondsField(string[] parts, int offset, out CronField seconds)
    {
        if (offset == 0)
        {
            seconds = CronField.Single(0);
            return true;
        }

        return CronField.TryParse(parts[0], 0, 59, CronFieldKind.Second, out seconds);
    }

    private static bool TryParseCalendarAndTimeFields(
        string[] parts,
        int offset,
        CronField seconds,
        out CronScheduleFields fields
    )
    {
        fields = default;
        if (
            !CronField.TryParse(parts[offset], 0, 59, CronFieldKind.Minute, out var minutes)
            || !CronField.TryParse(parts[offset + 1], 0, 23, CronFieldKind.Hour, out var hours)
            || !CronField.TryParse(parts[offset + 2], 1, 31, CronFieldKind.DayOfMonth, out var dom)
            || !CronField.TryParse(parts[offset + 3], 1, 12, CronFieldKind.Month, out var months)
            || !CronField.TryParse(parts[offset + 4], 0, 6, CronFieldKind.DayOfWeek, out var dow)
        )
        {
            return false;
        }

        fields = new CronScheduleFields(seconds, minutes, hours, dom, months, dow);
        return true;
    }

    private static bool IsNeverSentinel(string trimmed)
    {
        return string.Equals(trimmed, NeverExpression, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ManualExpression, StringComparison.OrdinalIgnoreCase);
    }
}
