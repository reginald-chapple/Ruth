namespace Ruth.SharedKernel.Security;

/// <summary>
/// Pure-BCL open-redirect and OAuth 2.0 / OIDC redirect-URI validator (RFC 6749 §3.1.2 &amp; RFC 9700).
/// </summary>
public static class SafeRedirectValidator
{
    private static readonly string[] _forbiddenSchemes =
    [
        "javascript",
        "data",
        "vbscript",
        "file",
        "blob",
        "ftp",
        "gopher",
        "dict",
        "ws",
        "wss",
    ];

    /// <summary>
    /// Validates that <paramref name="returnUrl" /> is null/empty or a strict root-relative local path
    /// (<c lang="text">/...</c>) free of protocol-relative (<c lang="text">//</c>, <c lang="text">/\</c>), authority, or control-character bypasses.
    /// </summary>
    public static bool IsSafeLocalReturnPath(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return true;
        }

        if (returnUrl.Length > 1024 || returnUrl[0] != '/')
        {
            return false;
        }

        if (returnUrl.Length >= 2 && (returnUrl[1] == '/' || returnUrl[1] == '\\'))
        {
            return false;
        }

        foreach (var ch in returnUrl)
        {
            if (char.IsControl(ch) || char.IsWhiteSpace(ch) || ch is '\\' or '@')
            {
                return false;
            }
        }

        return !returnUrl.StartsWith("/%2f", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/%5c", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.Contains("://", StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates an OAuth 2.0 / OIDC registered redirect or callback URI, forbidding wildcards, fragments,
    /// userinfo, path traversal, non-loopback cleartext HTTP, and dangerous executable schemes.
    /// </summary>
    public static bool IsSafeRegisteredRedirectUri(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return false;
        }

        var trimmed = redirectUri.Trim();
        var isAllowedBody = trimmed.StartsWith('/')
            ? IsSafeLocalReturnPath(trimmed)
            : Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed)
                && string.IsNullOrEmpty(parsed.UserInfo)
                && string.IsNullOrEmpty(parsed.Fragment)
                && IsAllowedRedirectScheme(parsed);
        return trimmed.Length <= 1024
            && !HasForbiddenRedirectCharacters(trimmed)
            && !HasDotDotSegment(trimmed)
            && isAllowedBody;
    }

    /// <summary>
    /// Enforces RFC 9700 exact character-for-character ordinal redirect URI comparison.
    /// </summary>
    public static bool MatchesExactRedirectUri(
        string registeredRedirectUri,
        string? candidateRedirectUri
    )
    {
        return !string.IsNullOrEmpty(registeredRedirectUri)
            && !string.IsNullOrEmpty(candidateRedirectUri)
            && string.Equals(registeredRedirectUri, candidateRedirectUri, StringComparison.Ordinal);
    }

    private static bool HasForbiddenRedirectCharacters(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsControl(ch) || char.IsWhiteSpace(ch) || ch is '*' or '#' or '\\' or '@')
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDotDotSegment(string value)
    {
        return value.Contains("/..", StringComparison.Ordinal)
            || value.Contains("../", StringComparison.Ordinal)
            || value.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase)
            || value.Contains("%2e.", StringComparison.OrdinalIgnoreCase)
            || value.Contains(".%2e", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedRedirectScheme(Uri uri)
    {
        var scheme = uri.Scheme;
        if (string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(uri.Host);
        }

        if (string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal)
                || string.Equals(uri.Host, "[::1]", StringComparison.Ordinal)
                || string.Equals(uri.Host, "::1", StringComparison.Ordinal);
        }

        foreach (var forbidden in _forbiddenSchemes)
        {
            if (string.Equals(scheme, forbidden, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return IsValidCustomNativeScheme(scheme);
    }

    private static bool IsValidCustomNativeScheme(string scheme)
    {
        if (scheme.Length is < 2 or > 40 || !char.IsAsciiLetter(scheme[0]))
        {
            return false;
        }

        foreach (var ch in scheme)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '+' and not '-' and not '.')
            {
                return false;
            }
        }

        return true;
    }
}
