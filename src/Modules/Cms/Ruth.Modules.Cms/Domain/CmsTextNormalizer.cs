using System.Text;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Pure deterministic text, slug, host, route, and URL normalization helpers for CMS entities.</summary>
public static class CmsTextNormalizer
{
    /// <summary>Normalizes a slug/key to lowercase URL-safe characters.</summary>
    public static string NormalizeSlug(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var lastDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                _ = sb.Append(char.ToLowerInvariant(ch));
                lastDash = false;
            }
            else if (ch is '-' or '_' or ' ' or '.' or '/')
            {
                if (!lastDash && sb.Length > 0)
                {
                    _ = sb.Append('-');
                    lastDash = true;
                }
            }
        }

        return sb.ToString().Trim('-');
    }

    /// <summary>Normalizes a dotted/dashed key such as cms.html-content.</summary>
    public static string NormalizeKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '.' or '-' or '_')
            {
                _ = sb.Append(char.ToLowerInvariant(ch));
            }
            else if (ch == ' ')
            {
                _ = sb.Append('-');
            }
        }

        return sb.ToString().Trim('-', '.');
    }

    /// <summary>Normalizes an HTTP host domain (strips scheme, port, trailing slashes, and lowercases).</summary>
    public static string NormalizeHost(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw.Trim().ToLowerInvariant();
        if (value.StartsWith("https://", StringComparison.Ordinal))
        {
            value = value["https://".Length..];
        }
        else if (value.StartsWith("http://", StringComparison.Ordinal))
        {
            value = value["http://".Length..];
        }

        var slashIdx = value.IndexOf('/', StringComparison.Ordinal);
        if (slashIdx >= 0)
        {
            value = value[..slashIdx];
        }

        var colonIdx = value.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx >= 0)
        {
            value = value[..colonIdx];
        }

        return value.Trim('.');
    }

    /// <summary>Normalizes a CMS page route path without leading/trailing slashes (empty for root).</summary>
    public static string NormalizeRoute(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        var queryIdx = trimmed.IndexOf('?', StringComparison.Ordinal);
        if (queryIdx >= 0)
        {
            trimmed = trimmed[..queryIdx];
        }

        return trimmed.Trim('/');
    }

    /// <summary>Validates that a URL is either root-relative (/...) or http/https without backslash or userinfo bypasses.</summary>
    public static bool IsSafeHttpOrRelativeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch) || ch is '\\' or '@')
            {
                return false;
            }
        }

        var isRootRelative =
            trimmed.StartsWith('/')
            && !trimmed.StartsWith("//", StringComparison.Ordinal)
            && !trimmed.StartsWith("/%2f", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("/%5c", StringComparison.OrdinalIgnoreCase);
        return isRootRelative
            || (
                Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                && string.IsNullOrEmpty(uri.UserInfo)
                && (
                    string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        uri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );
    }

    /// <summary>Validates a CSS custom property name (--token-name).</summary>
    public static bool IsValidCssCustomPropertyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        if (name.Length is < 3 or > 80)
        {
            return false;
        }

        for (var i = 2; i < name.Length; i++)
        {
            var ch = name[i];
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Sanitizes a CSS custom property value so it cannot inject rules or closing braces.</summary>
    public static string SanitizeCssPropertyValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed
            .Replace(";", string.Empty, StringComparison.Ordinal)
            .Replace("{", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal)
            .Replace("</style>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</style", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal);
    }
}
