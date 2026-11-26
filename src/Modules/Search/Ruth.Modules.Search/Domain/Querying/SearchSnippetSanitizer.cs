using System.Net;

namespace Ruth.Modules.Search.Domain.Querying;

/// <summary>
/// Sanitizes PostgreSQL ts_headline output so only safe &lt;mark&gt; highlight spans remain,
/// preventing HTML/XSS injection from indexed document bodies.
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

        var encoded = WebUtility.HtmlEncode(rawHeadline);
        return encoded
            .Replace(StartMarker, "<mark>", StringComparison.Ordinal)
            .Replace(StopMarker, "</mark>", StringComparison.Ordinal);
    }
}
