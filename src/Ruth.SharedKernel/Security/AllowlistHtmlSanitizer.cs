using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Strict allowlist HTML fragment sanitizer blocking script, SVG/MathML, iframe/object, event handlers,
/// CSS expressions, protocol-relative/javascript/data URLs, and malformed tag injection.
/// </summary>
public static class AllowlistHtmlSanitizer
{
    /// <summary>Sanitizes an HTML fragment against the strict element, attribute, URL, and CSS allowlists.</summary>
    public static string SanitizeFragment(string? rawHtml, bool allowMergeTokensInUrls = false)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(rawHtml.Length);
        var index = 0;
        while (index < rawHtml.Length)
        {
            var lt = rawHtml.IndexOf('<', index);
            if (lt < 0)
            {
                HtmlTagAttributeLexer.AppendSafeText(sb, rawHtml.AsSpan(index));
                break;
            }

            if (lt > index)
            {
                HtmlTagAttributeLexer.AppendSafeText(sb, rawHtml.AsSpan(index, lt - index));
            }

            index = ConsumeTagOrComment(rawHtml, lt, allowMergeTokensInUrls, sb);
        }

        return sb.ToString();
    }

    private static int ConsumeTagOrComment(
        string html,
        int ltIndex,
        bool allowMergeTokensInUrls,
        StringBuilder sb
    )
    {
        if (HtmlTagAttributeLexer.StartsWithOrdinal(html, ltIndex, "<!--"))
        {
            var endComment = html.IndexOf("-->", ltIndex + 4, StringComparison.Ordinal);
            return endComment < 0 ? html.Length : endComment + 3;
        }

        if (
            HtmlTagAttributeLexer.StartsWithOrdinal(html, ltIndex, "<!")
            || HtmlTagAttributeLexer.StartsWithOrdinal(html, ltIndex, "<?")
        )
        {
            var closeDecl = html.IndexOf('>', ltIndex + 2);
            return closeDecl < 0 ? html.Length : closeDecl + 1;
        }

        var gtIndex = HtmlTagAttributeLexer.FindTagEnd(html, ltIndex + 1);
        if (gtIndex < 0)
        {
            _ = sb.Append("&lt;");
            return ltIndex + 1;
        }

        var tagSlice = html.AsSpan(ltIndex + 1, gtIndex - ltIndex - 1).Trim();
        return ProcessParsedTag(html, gtIndex + 1, tagSlice, allowMergeTokensInUrls, sb);
    }

    private static int ProcessParsedTag(
        string html,
        int nextIndex,
        ReadOnlySpan<char> tagSlice,
        bool allowMergeTokensInUrls,
        StringBuilder sb
    )
    {
        if (tagSlice.IsEmpty)
        {
            return nextIndex;
        }

        var isClosing = tagSlice[0] == '/';
        var body = isClosing ? tagSlice[1..].TrimStart() : tagSlice;
        var selfClosing = !isClosing && body.EndsWith('/');
        if (selfClosing)
        {
            body = body[..^1].TrimEnd();
        }

        var tagName = HtmlTagAttributeLexer.ReadIdentifier(body, out var attrSpan);
        if (string.IsNullOrEmpty(tagName))
        {
            return nextIndex;
        }

        var lowerTag = tagName.ToLowerInvariant();
        if (HtmlSanitizerRules.DropWithContentElements.Contains(lowerTag))
        {
            return isClosing || selfClosing
                ? nextIndex
                : HtmlTagAttributeLexer.SkipDroppedElementContent(html, nextIndex, lowerTag);
        }

        if (!HtmlSanitizerRules.AllowedElements.Contains(lowerTag))
        {
            return nextIndex;
        }

        if (isClosing)
        {
            if (!HtmlSanitizerRules.VoidElements.Contains(lowerTag))
            {
                _ = sb.Append("</").Append(lowerTag).Append('>');
            }

            return nextIndex;
        }

        EmitAllowedOpeningTag(sb, lowerTag, attrSpan, allowMergeTokensInUrls);
        return nextIndex;
    }

    private static void EmitAllowedOpeningTag(
        StringBuilder sb,
        string lowerTag,
        ReadOnlySpan<char> attrSpan,
        bool allowMergeTokensInUrls
    )
    {
        var attributes = HtmlTagAttributeLexer.ParseAttributes(attrSpan);
        var filtered = FilterAttributes(lowerTag, attributes, allowMergeTokensInUrls);

        if (
            string.Equals(lowerTag, "img", StringComparison.Ordinal)
            && !filtered.Exists(static a => string.Equals(a.Name, "src", StringComparison.Ordinal))
        )
        {
            return;
        }

        _ = sb.Append('<').Append(lowerTag);
        foreach (var (name, val) in filtered)
        {
            _ = sb.Append(' ')
                .Append(name)
                .Append("=\"")
                .Append(HtmlTagAttributeLexer.EncodeAttributeValue(val, allowMergeTokensInUrls))
                .Append('"');
        }

        _ = HtmlSanitizerRules.VoidElements.Contains(lowerTag) ? sb.Append(" />") : sb.Append('>');
    }

    private static List<(string Name, string Value)> FilterAttributes(
        string lowerTag,
        List<(string Name, string Value)> rawAttrs,
        bool allowMergeTokensInUrls
    )
    {
        var accepted = new List<(string Name, string Value)>(rawAttrs.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasTargetBlank = false;

        foreach (var (rawName, rawVal) in rawAttrs)
        {
            var lowerAttr = rawName.ToLowerInvariant();
            if (!HtmlSanitizerRules.IsAttributeAllowed(lowerTag, lowerAttr) || !seen.Add(lowerAttr))
            {
                continue;
            }

            var sanitizedVal = SanitizeSingleAttribute(
                lowerTag,
                lowerAttr,
                rawVal,
                allowMergeTokensInUrls,
                ref hasTargetBlank
            );
            if (sanitizedVal is not null)
            {
                accepted.Add((lowerAttr, sanitizedVal));
            }
        }

        if (hasTargetBlank && seen.Add("rel"))
        {
            accepted.Add(("rel", "noopener noreferrer"));
        }

        return accepted;
    }

    private static string? SanitizeSingleAttribute(
        string lowerTag,
        string lowerAttr,
        string rawVal,
        bool allowMergeTokensInUrls,
        ref bool hasTargetBlank
    )
    {
        if (
            string.Equals(lowerAttr, "href", StringComparison.Ordinal)
            || string.Equals(lowerAttr, "src", StringComparison.Ordinal)
        )
        {
            return HtmlSanitizerRules.SanitizeUrlValue(
                rawVal,
                isImageSrc: string.Equals(lowerAttr, "src", StringComparison.Ordinal),
                allowMergeTokensInUrls
            );
        }

        if (string.Equals(lowerAttr, "style", StringComparison.Ordinal))
        {
            var cleanCss = CssStyleAllowlistSanitizer.SanitizeStyleAttribute(rawVal);
            return string.IsNullOrEmpty(cleanCss) ? null : cleanCss;
        }

        if (string.Equals(lowerAttr, "target", StringComparison.Ordinal))
        {
            if (string.Equals(rawVal.Trim(), "_blank", StringComparison.OrdinalIgnoreCase))
            {
                hasTargetBlank = true;
                return "_blank";
            }

            return null;
        }

        return
            string.Equals(lowerTag, "a", StringComparison.Ordinal)
            && string.Equals(lowerAttr, "rel", StringComparison.Ordinal)
            ? "noopener noreferrer"
            : HtmlTagAttributeLexer.StripControlCharacters(rawVal);
    }
}
