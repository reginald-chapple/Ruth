using System.Security.Cryptography;
using System.Text;

namespace Ruth.Modules.Search.Domain;

internal static class SearchTextNormalizer
{
    public static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim().ToLowerInvariant();
        var sb = new StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                _ = sb.Append(ch);
                lastWasDash = false;
            }
            else if (ShouldAppendDash(ch, lastWasDash, sb.Length))
            {
                _ = sb.Append('-');
                lastWasDash = true;
            }
        }

        return sb.ToString().Trim('-');
    }

    private static bool ShouldAppendDash(char ch, bool lastWasDash, int currentLength)
    {
        return !lastWasDash
            && currentLength > 0
            && (ch == '-' || ch == '_' || char.IsWhiteSpace(ch));
    }

    public static string ComputeContentHash(
        string title,
        string? subtitle,
        string? summary,
        string? bodyText,
        string tagsText,
        string? urlPath,
        bool isPublic,
        string facetsJson
    )
    {
        var payload = string.Join(
            '\u001F',
            title,
            subtitle ?? string.Empty,
            summary ?? string.Empty,
            bodyText ?? string.Empty,
            tagsText,
            urlPath ?? string.Empty,
            isPublic ? "1" : "0",
            facetsJson
        );
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}
