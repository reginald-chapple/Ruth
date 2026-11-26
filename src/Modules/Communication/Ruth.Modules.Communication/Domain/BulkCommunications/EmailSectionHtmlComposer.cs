using System.Net;
using System.Text;
using Ruth.Modules.Communication.Contracts.BulkCommunications;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Compiles structured <see cref="EmailSection" /> blocks into standards-compliant HTML5 email fragments.</summary>
public static class EmailSectionHtmlComposer
{
    private const string DefaultAccent = "#0f6cbd";

    /// <summary>Renders ordered <paramref name="sections" /> into a responsive HTML fragment ready for merge-token evaluation.</summary>
    public static string ComposeBodyMarkup(
        IReadOnlyList<EmailSection> sections,
        bool includeUnsubscribeFooter = true
    )
    {
        ArgumentNullException.ThrowIfNull(sections);

        var sb = new StringBuilder(1024);
        foreach (var section in sections.OrderBy(static s => s.Order))
        {
            AppendSection(sb, section);
        }

        if (includeUnsubscribeFooter)
        {
            AppendUnsubscribeFooter(sb);
        }

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, EmailSection section)
    {
        var accent = section.AccentColorHex ?? DefaultAccent;
        switch (section.Kind)
        {
            case EmailSectionKind.HeroBanner:
                AppendHero(sb, section, accent);
                break;
            case EmailSectionKind.RichText:
                AppendRichText(sb, section);
                break;
            case EmailSectionKind.CallToActionButton:
                AppendCtaButton(sb, section, accent);
                break;
            case EmailSectionKind.ImageBlock:
                AppendImage(sb, section);
                break;
            case EmailSectionKind.TwoColumnText:
                AppendTwoColumns(sb, section);
                break;
            case EmailSectionKind.Divider:
                _ = sb.Append(
                    "<hr style=\"border:0;border-top:1px solid #e5e7eb;margin:20px 0;\" />"
                );
                break;
            case EmailSectionKind.Footer:
                AppendCustomFooter(sb, section);
                break;
            default:
                break;
        }
    }

    private static void AppendHero(StringBuilder sb, EmailSection section, string accent)
    {
        _ = sb.Append(
                "<div style=\"padding:20px 24px;margin:0 0 18px;border-radius:6px;background-color:"
            )
            .Append(accent)
            .Append(";color:#ffffff;\">");
        if (!string.IsNullOrWhiteSpace(section.Heading))
        {
            _ = sb.Append("<h2 style=\"margin:0 0 8px;font-size:20px;line-height:1.3;\">")
                .Append(EncodePreservingMergeTokens(section.Heading))
                .Append("</h2>");
        }

        if (!string.IsNullOrWhiteSpace(section.BodyMarkup))
        {
            _ = sb.Append("<p style=\"margin:0;font-size:15px;line-height:1.5;\">")
                .Append(section.BodyMarkup)
                .Append("</p>");
        }

        _ = sb.Append("</div>");
    }

    private static void AppendRichText(StringBuilder sb, EmailSection section)
    {
        _ = sb.Append("<div style=\"margin:0 0 16px;\">");
        if (!string.IsNullOrWhiteSpace(section.Heading))
        {
            _ = sb.Append("<h3 style=\"margin:0 0 8px;font-size:18px;color:#111827;\">")
                .Append(EncodePreservingMergeTokens(section.Heading))
                .Append("</h3>");
        }

        if (!string.IsNullOrWhiteSpace(section.BodyMarkup))
        {
            _ = sb.Append("<div style=\"font-size:15px;line-height:1.6;color:#1f2937;\">")
                .Append(section.BodyMarkup)
                .Append("</div>");
        }

        _ = sb.Append("</div>");
    }

    private static void AppendCtaButton(StringBuilder sb, EmailSection section, string accent)
    {
        if (
            string.IsNullOrWhiteSpace(section.ButtonLabel)
            || !EmailSection.IsSafeUrlOrMergeToken(section.ButtonUrl)
        )
        {
            return;
        }

        _ = sb.Append("<div style=\"margin:20px 0;\"><a href=\"")
            .Append(EncodeAttributePreservingTokens(section.ButtonUrl!))
            .Append("\" style=\"display:inline-block;padding:12px 22px;background-color:")
            .Append(accent)
            .Append(";color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600;\">")
            .Append(EncodePreservingMergeTokens(section.ButtonLabel))
            .Append("</a></div>");
    }

    private static void AppendImage(StringBuilder sb, EmailSection section)
    {
        if (!EmailSection.IsSafeUrlOrMergeToken(section.ImageUrl))
        {
            return;
        }

        var alt = string.IsNullOrWhiteSpace(section.ImageAltText)
            ? "Illustration"
            : WebUtility.HtmlEncode(section.ImageAltText);
        _ = sb.Append("<div style=\"margin:16px 0;\"><img src=\"")
            .Append(EncodeAttributePreservingTokens(section.ImageUrl!))
            .Append("\" alt=\"")
            .Append(alt)
            .Append(
                "\" style=\"max-width:100%;height:auto;border-radius:6px;display:block;\" /></div>"
            );
    }

    private static void AppendTwoColumns(StringBuilder sb, EmailSection section)
    {
        _ = sb.Append(
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:16px 0;\"><tr>"
            )
            .Append(
                "<td width=\"50%\" valign=\"top\" style=\"padding-right:10px;font-size:14px;line-height:1.5;\">"
            )
            .Append(section.BodyMarkup ?? string.Empty)
            .Append("</td>")
            .Append(
                "<td width=\"50%\" valign=\"top\" style=\"padding-left:10px;font-size:14px;line-height:1.5;\">"
            )
            .Append(section.SecondaryColumnMarkup ?? string.Empty)
            .Append("</td></tr></table>");
    }

    private static void AppendCustomFooter(StringBuilder sb, EmailSection section)
    {
        _ = sb.Append(
                "<div style=\"margin:20px 0 0;padding-top:12px;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;\">"
            )
            .Append(section.BodyMarkup ?? string.Empty)
            .Append("</div>");
    }

    private static void AppendUnsubscribeFooter(StringBuilder sb)
    {
        _ = sb.Append(
            "<p style=\"margin:24px 0 0;padding-top:12px;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;\">"
                + "Manage your preferences at <a href=\"{{ PreferenceCenterUrl | default:'/communication/preferences' }}\" style=\"color:#0f6cbd;\">Communication Preferences</a> "
                + "or <a href=\"{{ UnsubscribeUrl | default:'/communication/preferences' }}\" style=\"color:#0f6cbd;\">Unsubscribe</a>."
                + "</p>"
        );
    }

    private static string EncodePreservingMergeTokens(string text)
    {
        return text.Contains("{{", StringComparison.Ordinal) ? text : WebUtility.HtmlEncode(text);
    }

    private static string EncodeAttributePreservingTokens(string url)
    {
        return url.StartsWith("{{", StringComparison.Ordinal) ? url : WebUtility.HtmlEncode(url);
    }
}
