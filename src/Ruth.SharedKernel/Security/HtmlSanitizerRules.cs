using System.Collections.Frozen;
using System.Net;
using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>Tag, attribute, and URI scheme allowlists backing <see cref="AllowlistHtmlSanitizer" />.</summary>
internal static class HtmlSanitizerRules
{
    public static readonly FrozenSet<string> DropWithContentElements = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "script",
        "style",
        "svg",
        "math",
        "iframe",
        "frame",
        "frameset",
        "object",
        "embed",
        "applet",
        "template",
        "noscript",
        "noembed",
        "textarea",
        "xmp"
    );

    public static readonly FrozenSet<string> VoidElements = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "br",
        "hr",
        "img"
    );

    public static readonly FrozenSet<string> AllowedElements = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "p",
        "br",
        "hr",
        "div",
        "span",
        "section",
        "article",
        "header",
        "footer",
        "blockquote",
        "pre",
        "code",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "strong",
        "b",
        "em",
        "i",
        "u",
        "s",
        "sub",
        "sup",
        "small",
        "mark",
        "ul",
        "ol",
        "li",
        "dl",
        "dt",
        "dd",
        "table",
        "thead",
        "tbody",
        "tfoot",
        "tr",
        "th",
        "td",
        "a",
        "img"
    );

    private static readonly FrozenSet<string> _globalAttributes = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "class",
        "title",
        "role",
        "dir",
        "lang",
        "aria-label",
        "aria-hidden",
        "style"
    );

    private static readonly FrozenSet<string> _tableAttributes = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "width",
        "cellpadding",
        "cellspacing",
        "valign",
        "align",
        "colspan",
        "rowspan",
        "scope"
    );

    public static bool IsAttributeAllowed(string tag, string attrName)
    {
        return !string.IsNullOrWhiteSpace(attrName)
            && !attrName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
            && (
                _globalAttributes.Contains(attrName)
                || (
                    string.Equals(tag, "a", StringComparison.OrdinalIgnoreCase)
                        ? string.Equals(attrName, "href", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attrName, "target", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attrName, "rel", StringComparison.OrdinalIgnoreCase)
                    : string.Equals(tag, "img", StringComparison.OrdinalIgnoreCase)
                        ? string.Equals(attrName, "src", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attrName, "alt", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attrName, "width", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attrName, "height", StringComparison.OrdinalIgnoreCase)
                    : IsTableElement(tag) && _tableAttributes.Contains(attrName)
                )
            );
    }

    public static string? SanitizeUrlValue(string rawValue, bool isImageSrc, bool allowMergeTokens)
    {
        var trimmed = rawValue.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (
            allowMergeTokens
            && trimmed.StartsWith("{{", StringComparison.Ordinal)
            && trimmed.EndsWith("}}", StringComparison.Ordinal)
            && !trimmed[2..^2].Contains("{{", StringComparison.Ordinal)
        )
        {
            return trimmed;
        }

        var decoded = CollapseControlAndWhitespace(WebUtility.HtmlDecode(trimmed));
        return string.IsNullOrEmpty(decoded) ? null
            : decoded.StartsWith('/') && !decoded.StartsWith("//", StringComparison.Ordinal)
                ? StripControlChars(trimmed)
            : !isImageSrc && decoded.StartsWith('#') ? StripControlChars(trimmed)
            : IsAllowedAbsoluteScheme(decoded, isImageSrc) ? StripControlChars(trimmed)
            : null;
    }

    private static bool IsAllowedAbsoluteScheme(string decoded, bool isImageSrc)
    {
        return decoded.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || decoded.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || (
                !isImageSrc
                && (
                    decoded.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || decoded.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                )
            );
    }

    private static bool IsTableElement(string tag)
    {
        return string.Equals(tag, "table", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tag, "tr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tag, "td", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tag, "th", StringComparison.OrdinalIgnoreCase);
    }

    private static string CollapseControlAndWhitespace(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (!char.IsControl(ch) && !char.IsWhiteSpace(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string StripControlChars(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (!char.IsControl(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
