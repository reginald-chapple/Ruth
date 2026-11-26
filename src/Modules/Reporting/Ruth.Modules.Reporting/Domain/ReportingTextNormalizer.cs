using System.Text;

namespace Ruth.Modules.Reporting.Domain;

internal static class ReportingTextNormalizer
{
    public static string NormalizeKey(string? value)
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
            if (IsAllowedKeyChar(ch))
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

    private static bool IsAllowedKeyChar(char ch)
    {
        return char.IsAsciiLetterOrDigit(ch) || ch == '.' || ch == '_';
    }

    private static bool ShouldAppendDash(char ch, bool lastWasDash, int currentLength)
    {
        return !lastWasDash && currentLength > 0 && (ch == '-' || char.IsWhiteSpace(ch));
    }

    public static string SanitizeFileName(string? value, string fallback)
    {
        var key = NormalizeKey(value);
        return string.IsNullOrWhiteSpace(key) ? fallback : key.Replace('.', '-');
    }
}
