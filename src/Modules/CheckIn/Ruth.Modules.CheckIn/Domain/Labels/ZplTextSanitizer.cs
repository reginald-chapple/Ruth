using System.Collections.Frozen;
using System.Text;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>
/// Prevents Zebra ZPL II command injection by escaping ZPL control prefixes (^, ~, \) and hex indicator (_)
/// using ZPL ^FH_ hexadecimal field encoding.
/// </summary>
public static class ZplTextSanitizer
{
    private static readonly FrozenDictionary<char, string> _escapeMap = new Dictionary<char, string>
    {
        ['^'] = "_5E",
        ['~'] = "_7E",
        ['\\'] = "_5C",
        ['_'] = "_5F",
        ['\r'] = " ",
        ['\n'] = " ",
    }.ToFrozenDictionary();

    /// <summary>Escapes arbitrary text for safe inclusion inside a ^FH_^FD...^FS ZPL field block.</summary>
    public static string EscapeForZplField(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(raw.Length + 8);
        foreach (var ch in raw)
        {
            AppendEscapedChar(sb, ch);
        }

        return sb.ToString();
    }

    private static void AppendEscapedChar(StringBuilder sb, char ch)
    {
        if (_escapeMap.TryGetValue(ch, out var replacement))
        {
            _ = sb.Append(replacement);
        }
        else if (!char.IsControl(ch))
        {
            _ = sb.Append(ch);
        }
    }
}
