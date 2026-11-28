using System.Net;
using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>Low-level tag/attribute lexer and encoding helpers for <see cref="AllowlistHtmlSanitizer" />.</summary>
internal static class HtmlTagAttributeLexer
{
    public static List<(string Name, string Value)> ParseAttributes(ReadOnlySpan<char> span)
    {
        var list = new List<(string Name, string Value)>();
        var idx = 0;
        while (idx < span.Length)
        {
            while (idx < span.Length && char.IsWhiteSpace(span[idx]))
            {
                idx++;
            }

            if (idx >= span.Length)
            {
                break;
            }

            var nameStart = idx;
            while (
                idx < span.Length
                && !char.IsWhiteSpace(span[idx])
                && span[idx] is not '=' and not '/' and not '"' and not '\''
            )
            {
                idx++;
            }

            if (idx == nameStart)
            {
                idx++;
                continue;
            }

            var name = span[nameStart..idx].ToString();
            var value = ReadOptionalAttributeValue(span, ref idx);
            list.Add((name, value));
        }

        return list;
    }

    public static int SkipDroppedElementContent(string html, int startIndex, string lowerTag)
    {
        var searchFrom = startIndex;
        var closeToken = $"</{lowerTag}";
        while (searchFrom < html.Length)
        {
            var closePos = html.IndexOf(closeToken, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (closePos < 0)
            {
                return html.Length;
            }

            var afterName = closePos + closeToken.Length;
            if (
                afterName < html.Length
                && (html[afterName] == '>' || char.IsWhiteSpace(html[afterName]))
            )
            {
                var gt = html.IndexOf('>', afterName);
                return gt < 0 ? html.Length : gt + 1;
            }

            searchFrom = afterName;
        }

        return html.Length;
    }

    public static int FindTagEnd(string html, int startIndex)
    {
        var activeQuote = '\0';
        for (var i = startIndex; i < html.Length; i++)
        {
            var ch = html[i];
            if (activeQuote != '\0')
            {
                if (ch == activeQuote)
                {
                    activeQuote = '\0';
                }

                continue;
            }

            if (ch is '"' or '\'')
            {
                activeQuote = ch;
            }
            else if (ch == '>')
            {
                return i;
            }
        }

        return -1;
    }

    public static string ReadIdentifier(ReadOnlySpan<char> body, out ReadOnlySpan<char> remainder)
    {
        var i = 0;
        while (i < body.Length && (char.IsAsciiLetterOrDigit(body[i]) || body[i] == '-'))
        {
            i++;
        }

        remainder = body[i..];
        return i == 0 ? string.Empty : body[..i].ToString();
    }

    public static void AppendSafeText(StringBuilder sb, ReadOnlySpan<char> text)
    {
        foreach (var ch in text)
        {
            if (ch == '\0')
            {
                continue;
            }

            _ = ch switch
            {
                '<' => sb.Append("&lt;"),
                '>' => sb.Append("&gt;"),
                _ => sb.Append(ch),
            };
        }
    }

    public static string EncodeAttributeValue(string value, bool preserveMergeTokens)
    {
        return
            preserveMergeTokens
            && value.StartsWith("{{", StringComparison.Ordinal)
            && value.EndsWith("}}", StringComparison.Ordinal)
            ? value
            : WebUtility.HtmlEncode(WebUtility.HtmlDecode(value));
    }

    public static string StripControlCharacters(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (!char.IsControl(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    public static bool StartsWithOrdinal(string source, int index, string prefix)
    {
        return index + prefix.Length <= source.Length
            && source.AsSpan(index, prefix.Length).SequenceEqual(prefix);
    }

    private static string ReadOptionalAttributeValue(ReadOnlySpan<char> span, ref int idx)
    {
        while (idx < span.Length && char.IsWhiteSpace(span[idx]))
        {
            idx++;
        }

        if (idx >= span.Length || span[idx] != '=')
        {
            return string.Empty;
        }

        idx++;
        while (idx < span.Length && char.IsWhiteSpace(span[idx]))
        {
            idx++;
        }

        return idx >= span.Length ? string.Empty : ReadQuotedOrUnquotedValue(span, ref idx);
    }

    private static string ReadQuotedOrUnquotedValue(ReadOnlySpan<char> span, ref int idx)
    {
        if (span[idx] is '"' or '\'')
        {
            var quote = span[idx++];
            var start = idx;
            while (idx < span.Length && span[idx] != quote)
            {
                idx++;
            }

            var val = span[start..idx].ToString();
            if (idx < span.Length)
            {
                idx++;
            }

            return val;
        }

        var unquotedStart = idx;
        while (idx < span.Length && !char.IsWhiteSpace(span[idx]))
        {
            idx++;
        }

        return span[unquotedStart..idx].ToString();
    }
}
