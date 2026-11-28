using System.Buffers;
using System.Collections.Frozen;
using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Allowlists safe inline CSS visual properties while blocking CSS expression(), url(), @import, and script vectors.
/// </summary>
internal static class CssStyleAllowlistSanitizer
{
    private static readonly SearchValues<char> _forbiddenValueChars = SearchValues.Create(
        "\\<>\"&"
    );
    private static readonly FrozenSet<string> _allowedProperties = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "color",
        "background-color",
        "font-size",
        "font-weight",
        "font-style",
        "font-family",
        "line-height",
        "text-align",
        "text-decoration",
        "margin",
        "margin-top",
        "margin-right",
        "margin-bottom",
        "margin-left",
        "padding",
        "padding-top",
        "padding-right",
        "padding-bottom",
        "padding-left",
        "border",
        "border-top",
        "border-right",
        "border-bottom",
        "border-left",
        "border-radius",
        "border-collapse",
        "width",
        "max-width",
        "min-width",
        "height",
        "display",
        "vertical-align"
    );

    private static readonly string[] _blockedValueTokens =
    [
        "url(",
        "expression",
        "javascript",
        "vbscript",
        "-moz-binding",
        "behavior",
        "@import",
        "/*",
        "*/",
    ];

    public static string SanitizeStyleAttribute(string? rawStyle)
    {
        if (string.IsNullOrWhiteSpace(rawStyle))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(rawStyle.Length);
        foreach (var declaration in rawStyle.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = declaration.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0 || colon >= declaration.Length - 1)
            {
                continue;
            }

            var prop = declaration[..colon].Trim().ToLowerInvariant();
            var val = declaration[(colon + 1)..].Trim();
            if (_allowedProperties.Contains(prop) && IsSafeCssValue(val))
            {
                if (sb.Length > 0)
                {
                    _ = sb.Append(';');
                }

                _ = sb.Append(prop).Append(':').Append(val);
            }
        }

        return sb.ToString();
    }

    private static bool IsSafeCssValue(string value)
    {
        if (
            string.IsNullOrWhiteSpace(value)
            || value.Length > 160
            || value.AsSpan().ContainsAny(_forbiddenValueChars)
        )
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return false;
            }
        }

        foreach (var blocked in _blockedValueTokens)
        {
            if (value.Contains(blocked, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
