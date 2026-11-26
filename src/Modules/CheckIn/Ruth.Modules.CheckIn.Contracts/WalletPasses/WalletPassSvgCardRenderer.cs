using System.Globalization;
using System.Net;
using System.Text;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Deterministic pure-C# SVG card renderer shared by preview, Apple, and Google wallet pass artifacts.</summary>
public static class WalletPassSvgCardRenderer
{
    /// <summary>Renders an accessible 340x460 wallet card SVG for the supplied pass specification.</summary>
    /// <param name="spec">Normalized wallet pass render specification.</param>
    public static string RenderSvg(WalletPassRenderSpecDto spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var header = WebUtility.HtmlEncode(spec.LogoText ?? spec.OrganizationName);
        var holder = WebUtility.HtmlEncode(
            spec.DynamicFields.HolderDisplayName ?? "Community Member"
        );
        var tier = WebUtility.HtmlEncode(spec.DynamicFields.MembershipTier ?? "Active");
        var balance = spec.DynamicFields.PunchOrCreditBalance.HasValue
            ? spec.DynamicFields.PunchOrCreditBalance.Value.ToString(CultureInfo.InvariantCulture)
            : "—";
        var summary = WebUtility.HtmlEncode(ResolveSecondarySummary(spec));
        var scanToken = WebUtility.HtmlEncode(spec.ScanToken);
        var serial = WebUtility.HtmlEncode(spec.SerialNumber);
        var styleBadge = FormatCardStyleBadge(spec.CardStyle);

        var sb = new StringBuilder(1536);
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 340 460\" width=\"340\" height=\"460\" role=\"img\" aria-label=\"{header} Wallet Pass\">"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<rect width=\"340\" height=\"460\" rx=\"20\" fill=\"{spec.BackgroundColorHex}\"/>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"40\" fill=\"{spec.ForegroundColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"16\" font-weight=\"700\">{header}</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"316\" y=\"40\" text-anchor=\"end\" fill=\"{spec.LabelColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"11\" font-weight=\"600\">{styleBadge}</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"84\" fill=\"{spec.LabelColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"10\" font-weight=\"600\">CARDHOLDER</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"108\" fill=\"{spec.ForegroundColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"22\" font-weight=\"700\">{holder}</text>"
        );
        AppendMiddleMetrics(sb, spec, tier, balance, summary);
        AppendBarcodeFooter(sb, scanToken, serial);
        _ = sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendMiddleMetrics(
        StringBuilder sb,
        WalletPassRenderSpecDto spec,
        string tier,
        string balance,
        string summary
    )
    {
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"154\" fill=\"{spec.LabelColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"10\" font-weight=\"600\">TIER</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"174\" fill=\"{spec.ForegroundColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"15\" font-weight=\"600\">{tier}</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"200\" y=\"154\" fill=\"{spec.LabelColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"10\" font-weight=\"600\">CREDITS / PUNCHES</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"200\" y=\"174\" fill=\"{spec.ForegroundColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"15\" font-weight=\"600\">{balance}</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"216\" fill=\"{spec.LabelColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"10\" font-weight=\"600\">NEXT SHIFT / EVENT / BOOKING</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"24\" y=\"236\" fill=\"{spec.ForegroundColorHex}\" font-family=\"system-ui,sans-serif\" font-size=\"13\">{summary}</text>"
        );
    }

    private static void AppendBarcodeFooter(StringBuilder sb, string scanToken, string serial)
    {
        _ = sb.Append(
            "<rect x=\"28\" y=\"278\" width=\"284\" height=\"150\" rx=\"12\" fill=\"#FFFFFF\"/>"
        );
        for (var i = 0; i < 24; i++)
        {
            var x = 52 + (i * 10);
            var width = ((scanToken[i % scanToken.Length] + i) % 3 == 0) ? 5 : 2;
            _ = sb.Append(
                CultureInfo.InvariantCulture,
                $"<rect x=\"{x}\" y=\"298\" width=\"{width}\" height=\"68\" fill=\"#111827\"/>"
            );
        }

        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"170\" y=\"392\" text-anchor=\"middle\" fill=\"#111827\" font-family=\"monospace\" font-size=\"13\" font-weight=\"700\">{scanToken}</text>"
        );
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"<text x=\"170\" y=\"412\" text-anchor=\"middle\" fill=\"#6B7280\" font-family=\"system-ui,sans-serif\" font-size=\"10\">{serial}</text>"
        );
    }

    private static string ResolveSecondarySummary(WalletPassRenderSpecDto spec)
    {
        return spec.DynamicFields.NextShiftOrBookingSummary
            ?? spec.DynamicFields.EventTicketSummary
            ?? spec.TemplateName;
    }

    private static string FormatCardStyleBadge(WalletPassCardStyle style)
    {
        return style switch
        {
            WalletPassCardStyle.MemberCredential => "MEMBER PASS",
            WalletPassCardStyle.PunchOrCreditCard => "PUNCH CARD",
            WalletPassCardStyle.VolunteerOrStaffShift => "SHIFT PASS",
            WalletPassCardStyle.EventTicket => "EVENT TICKET",
            _ => "SMART PASS",
        };
    }
}
