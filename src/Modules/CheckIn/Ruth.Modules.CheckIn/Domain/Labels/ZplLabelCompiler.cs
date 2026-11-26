using System.Globalization;
using System.Text;
using Ruth.Modules.CheckIn.Contracts.Labels;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>
/// Pure clean-room Zebra ZPL II compiler converting <see cref="CheckInLabelLayoutSchema" /> and
/// <see cref="CheckInLabelMergeContextDto" /> into deterministic ZPL II commands and WYSIWYG preview elements.
/// </summary>
public static class ZplLabelCompiler
{
    /// <summary>Compiles a label layout (or custom ZPL override) and merge context into ZPL II and visual elements.</summary>
    public static CheckInLabelPreviewResponse Compile(
        string labelName,
        CheckInLabelLayoutSchema layout,
        CheckInLabelMergeContextDto context,
        int copies = 1,
        string? customZplOverride = null
    )
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(context);

        var clampedCopies = Math.Clamp(copies, 1, 5);
        var widthDots = layout.WidthDots;
        var heightDots = layout.HeightDots;

        var visualElements = BuildVisualElements(layout, context, widthDots, heightDots);
        var zpl = !string.IsNullOrWhiteSpace(customZplOverride)
            ? ZplMergeTokenEvaluator.ResolveZplTemplate(customZplOverride, context)
            : EmitStructuredZpl(layout, context, widthDots, heightDots, clampedCopies);

        return new CheckInLabelPreviewResponse(
            string.IsNullOrWhiteSpace(labelName) ? "Check-In Label" : labelName.Trim(),
            widthDots,
            heightDots,
            layout.Dpi,
            zpl,
            visualElements
        );
    }

    private static List<RenderedLabelElementPreviewDto> BuildVisualElements(
        CheckInLabelLayoutSchema layout,
        CheckInLabelMergeContextDto context,
        int widthDots,
        int heightDots
    )
    {
        var list = new List<RenderedLabelElementPreviewDto>(layout.Elements.Count);
        foreach (var el in layout.Elements)
        {
            if (!ZplMergeTokenEvaluator.HasTruthyValue(el.ShowOnlyWhenFieldPresent, context))
            {
                continue;
            }

            var resolved = ZplMergeTokenEvaluator.ResolvePlainText(el.ContentTemplate, context);
            if (el.Kind != LabelElementKind.BoxOrRule && string.IsNullOrWhiteSpace(resolved))
            {
                continue;
            }

            list.Add(
                new RenderedLabelElementPreviewDto(
                    el.ElementKey,
                    el.Kind,
                    el.X,
                    el.Y,
                    el.Width,
                    el.Height,
                    ComputePercent(el.X, widthDots),
                    ComputePercent(el.Y, heightDots),
                    ComputePercent(el.Width, widthDots),
                    ComputePercent(el.Height, heightDots),
                    el.Alignment,
                    el.InvertPrint,
                    resolved
                )
            );
        }

        return list;
    }

    private static string EmitStructuredZpl(
        CheckInLabelLayoutSchema layout,
        CheckInLabelMergeContextDto context,
        int widthDots,
        int heightDots,
        int copies
    )
    {
        var sb = new StringBuilder(512);
        var inv = CultureInfo.InvariantCulture;
        _ = sb.AppendLine("^XA");
        _ = sb.AppendLine("^CI28");
        _ = sb.AppendLine(string.Create(inv, $"^PW{widthDots}"));
        _ = sb.AppendLine(string.Create(inv, $"^LL{heightDots}"));
        _ = sb.AppendLine("^LH0,0");

        foreach (var element in layout.Elements)
        {
            EmitSingleElement(sb, element, context);
        }

        _ = sb.AppendLine(string.Create(inv, $"^PQ{copies},0,1,Y"));
        _ = sb.Append("^XZ");
        return sb.ToString();
    }

    private static void EmitSingleElement(
        StringBuilder sb,
        CheckInLabelElement el,
        CheckInLabelMergeContextDto context
    )
    {
        if (!ZplMergeTokenEvaluator.HasTruthyValue(el.ShowOnlyWhenFieldPresent, context))
        {
            return;
        }

        if (el.Kind == LabelElementKind.BoxOrRule)
        {
            EmitBoxElement(sb, el);
            return;
        }

        var plain = ZplMergeTokenEvaluator.ResolvePlainText(el.ContentTemplate, context);
        if (string.IsNullOrWhiteSpace(plain))
        {
            return;
        }

        var escaped = ZplTextSanitizer.EscapeForZplField(plain);
        switch (el.Kind)
        {
            case LabelElementKind.BarcodeCode128:
                EmitBarcode128(sb, el, escaped);
                break;
            case LabelElementKind.QrCode:
                EmitQrCode(sb, el, escaped);
                break;
            case LabelElementKind.TextField:
            case LabelElementKind.SecurityCodeBlock:
            case LabelElementKind.AlertBanner:
            case LabelElementKind.BadgeIcon:
            case LabelElementKind.BoxOrRule:
            default:
                EmitTextOrBanner(sb, el, escaped);
                break;
        }
    }

    private static void EmitBoxElement(StringBuilder sb, CheckInLabelElement el)
    {
        var inv = CultureInfo.InvariantCulture;
        var thickness = el.InvertPrint ? el.Height : Math.Min(4, el.Height);
        _ = sb.AppendLine(
            string.Create(inv, $"^FO{el.X},{el.Y}^GB{el.Width},{el.Height},{thickness}^FS")
        );
    }

    private static void EmitBarcode128(StringBuilder sb, CheckInLabelElement el, string escaped)
    {
        var inv = CultureInfo.InvariantCulture;
        var barHeight = Math.Clamp(el.Height - 20, 24, 200);
        _ = sb.AppendLine(
            string.Create(inv, $"^FO{el.X},{el.Y}^BY2^BCN,{barHeight},Y,N,N^FH_^FD{escaped}^FS")
        );
    }

    private static void EmitQrCode(StringBuilder sb, CheckInLabelElement el, string escaped)
    {
        var inv = CultureInfo.InvariantCulture;
        _ = sb.AppendLine(string.Create(inv, $"^FO{el.X},{el.Y}^BQN,2,5^FH_^FDQA,{escaped}^FS"));
    }

    private static void EmitTextOrBanner(StringBuilder sb, CheckInLabelElement el, string escaped)
    {
        var inv = CultureInfo.InvariantCulture;
        if (el.InvertPrint)
        {
            _ = sb.AppendLine(
                string.Create(inv, $"^FO{el.X},{el.Y}^GB{el.Width},{el.Height},{el.Height}^FS")
            );
        }

        var alignCode = el.Alignment switch
        {
            LabelTextAlignment.Center => 'C',
            LabelTextAlignment.Right => 'R',
            LabelTextAlignment.Left => 'L',
            _ => 'L',
        };
        var textY = el.InvertPrint ? el.Y + Math.Max(4, (el.Height - el.FontHeight) / 2) : el.Y;
        var frPrefix = el.InvertPrint ? "^FR" : string.Empty;
        var fontSpec =
            el.FontWidth > 0
                ? string.Create(inv, $"^A0N,{el.FontHeight},{el.FontWidth}")
                : string.Create(inv, $"^A0N,{el.FontHeight}");

        _ = sb.AppendLine(
            string.Create(
                inv,
                $"^FO{el.X},{textY}{frPrefix}{fontSpec}^FB{el.Width},{el.MaxLines},0,{alignCode},0^FH_^FD{escaped}^FS"
            )
        );
    }

    private static decimal ComputePercent(int value, int total)
    {
        return total <= 0 ? 0m : Math.Round(value * 100m / total, 2, MidpointRounding.AwayFromZero);
    }
}
