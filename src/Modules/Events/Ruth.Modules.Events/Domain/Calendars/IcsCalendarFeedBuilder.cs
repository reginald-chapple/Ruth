using System.Globalization;
using System.Text;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Clean-room RFC 5545 VCALENDAR 2.0 feed compiler with UTF-8 75-octet content-line folding.</summary>
public static class IcsCalendarFeedBuilder
{
    private const int MaxLineOctets = 75;

    public static string BuildFeed(
        string calendarName,
        string? calendarDescription,
        IReadOnlyList<IcsCalendarEventEntry> entries
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarName);
        ArgumentNullException.ThrowIfNull(entries);

        var sb = new StringBuilder(capacity: 512 + (entries.Count * 256));
        AppendContentLine(sb, "BEGIN:VCALENDAR");
        AppendContentLine(sb, "VERSION:2.0");
        AppendContentLine(sb, "PRODID:-//Calendar//Event Calendars//EN");
        AppendContentLine(sb, "CALSCALE:GREGORIAN");
        AppendContentLine(sb, "METHOD:PUBLISH");
        AppendContentLine(sb, $"X-WR-CALNAME:{EscapeText(calendarName)}");
        if (!string.IsNullOrWhiteSpace(calendarDescription))
        {
            AppendContentLine(sb, $"X-WR-CALDESC:{EscapeText(calendarDescription)}");
        }

        foreach (var entry in entries.OrderBy(static e => e.StartUtc))
        {
            AppendEventComponent(sb, entry);
        }

        AppendContentLine(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    private static void AppendEventComponent(StringBuilder sb, IcsCalendarEventEntry entry)
    {
        AppendContentLine(sb, "BEGIN:VEVENT");
        AppendContentLine(sb, $"UID:{EscapeText(entry.Uid)}");
        AppendContentLine(sb, $"DTSTAMP:{FormatUtcTimestamp(entry.StampUtc)}");
        AppendContentLine(sb, $"DTSTART:{FormatUtcTimestamp(entry.StartUtc)}");
        var effectiveEnd =
            entry.EndUtc >= entry.StartUtc ? entry.EndUtc : entry.StartUtc.AddHours(1);
        AppendContentLine(sb, $"DTEND:{FormatUtcTimestamp(effectiveEnd)}");
        AppendContentLine(sb, $"SUMMARY:{EscapeText(entry.Summary)}");
        AppendOptionalEventFields(sb, entry);
        AppendContentLine(sb, entry.IsCancelled ? "STATUS:CANCELLED" : "STATUS:CONFIRMED");
        AppendContentLine(sb, "END:VEVENT");
    }

    private static void AppendOptionalEventFields(StringBuilder sb, IcsCalendarEventEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            AppendContentLine(sb, $"DESCRIPTION:{EscapeText(entry.Description)}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Location))
        {
            AppendContentLine(sb, $"LOCATION:{EscapeText(entry.Location)}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Url))
        {
            AppendContentLine(sb, $"URL:{entry.Url.Trim()}");
        }

        if (entry.Categories is { Count: > 0 })
        {
            var joined = string.Join(',', entry.Categories.Select(EscapeText));
            AppendContentLine(sb, $"CATEGORIES:{joined}");
        }
    }

    public static string EscapeText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("\r\n", @"\n", StringComparison.Ordinal)
            .Replace("\r", @"\n", StringComparison.Ordinal)
            .Replace("\n", @"\n", StringComparison.Ordinal)
            .Replace(";", @"\;", StringComparison.Ordinal)
            .Replace(",", @"\,", StringComparison.Ordinal);
    }

    private static string FormatUtcTimestamp(DateTimeOffset timestamp)
    {
        return timestamp
            .ToUniversalTime()
            .ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    }

    private static void AppendContentLine(StringBuilder sb, string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= MaxLineOctets)
        {
            _ = sb.Append(line).Append("\r\n");
            return;
        }

        var currentOctets = 0;
        var limit = MaxLineOctets;
        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            var runeOctets = Encoding.UTF8.GetByteCount(runeText);
            if (currentOctets + runeOctets > limit)
            {
                _ = sb.Append("\r\n ");
                currentOctets = 1;
                limit = MaxLineOctets;
            }

            _ = sb.Append(runeText);
            currentOctets += runeOctets;
        }

        _ = sb.Append("\r\n");
    }
}
