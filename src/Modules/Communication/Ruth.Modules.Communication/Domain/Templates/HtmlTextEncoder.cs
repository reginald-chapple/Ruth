using System.Buffers;
using System.Text;

namespace Ruth.Modules.Communication.Domain.Templates;

internal static class HtmlTextEncoder
{
    private static readonly SearchValues<char> _htmlSpecialChars = SearchValues.Create("&<>\"'");

    public static string Encode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var span = input.AsSpan();
        var firstIndex = span.IndexOfAny(_htmlSpecialChars);
        if (firstIndex < 0)
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 16);
        _ = builder.Append(span[..firstIndex]);

        for (var i = firstIndex; i < span.Length; i++)
        {
            _ = builder.Append(EncodeChar(span[i]));
        }

        return builder.ToString();
    }

    private static string EncodeChar(char ch)
    {
        return ch switch
        {
            '&' => "&amp;",
            '<' => "&lt;",
            '>' => "&gt;",
            '"' => "&quot;",
            '\'' => "&#39;",
            _ => ch.ToString(),
        };
    }
}
