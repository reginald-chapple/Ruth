using System.Net;
using System.Text;

namespace Ruth.Modules.Search.Domain.Querying;

/// <summary>
/// Sanitizes PostgreSQL ts_headline output so only balanced, attribute-free &lt;mark&gt;...&lt;/mark&gt;
/// highlight spans remain, preventing HTML/XSS injection and unclosed-tag DOM clobbering.
/// </summary>
public static class SearchSnippetSanitizer
{
    public const string StartMarker = "[[RUTH_MARK_START]]";
    public const string StopMarker = "[[RUTH_MARK_STOP]]";

    public static string SanitizeHighlightedSnippet(string? rawHeadline)
    {
        if (string.IsNullOrWhiteSpace(rawHeadline))
        {
            return string.Empty;
        }

        var scrubbed = StripControlCharacters(rawHeadline);
        var sb = new StringBuilder(scrubbed.Length + 16);
        var cursor = 0;

        while (cursor < scrubbed.Length)
        {
            var nextStart = scrubbed.IndexOf(StartMarker, cursor, StringComparison.Ordinal);
            var nextStop = scrubbed.IndexOf(StopMarker, cursor, StringComparison.Ordinal);

            if (nextStart < 0 && nextStop < 0)
            {
                _ = sb.Append(WebUtility.HtmlEncode(scrubbed[cursor..]));
                break;
            }

            if (nextStop >= 0 && (nextStart < 0 || nextStop < nextStart))
            {
                _ = sb.Append(WebUtility.HtmlEncode(scrubbed[cursor..nextStop]));
                cursor = nextStop + StopMarker.Length;
                continue;
            }

            _ = sb.Append(WebUtility.HtmlEncode(scrubbed[cursor..nextStart]));
            var innerStart = nextStart + StartMarker.Length;
            var matchingStop = scrubbed.IndexOf(StopMarker, innerStart, StringComparison.Ordinal);
            if (matchingStop < 0)
            {
                cursor = innerStart;
                continue;
            }

            var highlightedText = scrubbed[innerStart..matchingStop]
                .Replace(StartMarker, string.Empty, StringComparison.Ordinal);
            _ = sb.Append("<mark>")
                .Append(WebUtility.HtmlEncode(highlightedText))
                .Append("</mark>");
            cursor = matchingStop + StopMarker.Length;
        }

        return sb.ToString();
    }

    public static string? SanitizeUrlPath(string? rawUrlPath)
    {
        if (string.IsNullOrWhiteSpace(rawUrlPath))
        {
            return null;
        }

        var trimmed = rawUrlPath.Trim();
        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch))
            {
                return null;
            }
        }

        return trimmed.StartsWith('/')
            && !trimmed.StartsWith("//", StringComparison.Ordinal)
            && !trimmed.StartsWith("/\\", StringComparison.Ordinal)
                ? trimmed
            : trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? trimmed
            : null;
    }

    private static string StripControlCharacters(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch is '\r' or '\n' or '\t')
            {
                _ = sb.Append(' ');
            }
            else if (!char.IsControl(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
