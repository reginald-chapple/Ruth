using System.Buffers;
using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Prevents CRLF, control-character, encoded-newline, and SMTP envelope breakout injection
/// across outbound email headers, display names, subjects, and addresses.
/// </summary>
public static class EmailHeaderSanitizer
{
    private static readonly SearchValues<char> _forbiddenAddressPunctuation = SearchValues.Create(
        "<>\"',;\\()[]"
    );
    private static readonly string[] _encodedInjectionTokens =
    [
        "%0d",
        "%0a",
        "%00",
        "%250d",
        "%250a",
        "%2500",
    ];

    /// <summary>Returns true if <paramref name="raw" /> contains raw or URL-encoded CR/LF/NUL/line-separator control characters.</summary>
    public static bool ContainsHeaderInjection(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        foreach (var ch in raw)
        {
            if (IsLineOrControlChar(ch))
            {
                return true;
            }
        }

        foreach (var token in _encodedInjectionTokens)
        {
            if (raw.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Strips CR, LF, NUL, Unicode line separators, C0/C1 control characters, and encoded CRLF sequences from an email header value.</summary>
    public static string SanitizeHeaderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var working = StripEncodedNewlines(value);
        var sb = new StringBuilder(working.Length);
        var previousWasSpace = false;

        foreach (var ch in working)
        {
            if (IsLineOrControlChar(ch) || char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace && sb.Length > 0)
                {
                    _ = sb.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            _ = sb.Append(ch);
            previousWasSpace = false;
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Validates and normalizes an email address for header/SMTP envelope safety,
    /// rejecting CR/LF, encoded newlines, whitespace, quotes, angle-bracket breakouts, and malformed domains.
    /// </summary>
    public static bool TrySanitizeEmailAddress(string? rawAddress, out string sanitizedAddress)
    {
        sanitizedAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(rawAddress) || ContainsHeaderInjection(rawAddress))
        {
            return false;
        }

        var trimmed = rawAddress.Trim();
        if (trimmed.Length > 254 || HasForbiddenAddressCharacters(trimmed))
        {
            return false;
        }

        var atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);
        var lastAtIndex = trimmed.LastIndexOf('@');
        if (atIndex <= 0 || atIndex != lastAtIndex || atIndex >= trimmed.Length - 3)
        {
            return false;
        }

        var domain = trimmed[(atIndex + 1)..];
        if (
            domain.StartsWith('.')
            || domain.EndsWith('.')
            || (
                !domain.Contains('.', StringComparison.Ordinal)
                && !string.Equals(domain, "localhost", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return false;
        }

        sanitizedAddress = trimmed;
        return true;
    }

    private static bool HasForbiddenAddressCharacters(string candidate)
    {
        if (candidate.AsSpan().ContainsAny(_forbiddenAddressPunctuation))
        {
            return true;
        }

        foreach (var ch in candidate)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripEncodedNewlines(string raw)
    {
        var current = raw;
        foreach (var token in _encodedInjectionTokens)
        {
            current = current.Replace(token, " ", StringComparison.OrdinalIgnoreCase);
        }

        return current;
    }

    private static bool IsLineOrControlChar(char ch)
    {
        return char.IsControl(ch) || ch is '\u0085' or '\u2028' or '\u2029';
    }
}
