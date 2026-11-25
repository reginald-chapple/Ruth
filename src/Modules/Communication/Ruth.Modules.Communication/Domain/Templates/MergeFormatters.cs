using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using Ruth.Modules.Communication.Contracts.Templates;

namespace Ruth.Modules.Communication.Domain.Templates;

internal static class MergeFormatters
{
    private static readonly FrozenDictionary<
        string,
        Func<object?, string?, CultureInfo, TemplateOutputMode, FormattedMergeValue>
    > _handlers = new Dictionary<
        string,
        Func<object?, string?, CultureInfo, TemplateOutputMode, FormattedMergeValue>
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = ApplyDefault,
        ["upper"] = ApplyUpper,
        ["lower"] = ApplyLower,
        ["title"] = ApplyTitle,
        ["trim"] = ApplyTrim,
        ["truncate"] = ApplyTruncate,
        ["date"] = ApplyDate,
        ["currency"] = ApplyCurrency,
        ["number"] = ApplyNumber,
        ["phone"] = ApplyPhone,
        ["url_encode"] = ApplyUrlEncode,
        ["newlines_to_br"] = ApplyNewlinesToBr,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string name)
    {
        return _handlers.ContainsKey(name);
    }

    public static FormattedMergeValue ApplyChain(
        object? initialValue,
        IReadOnlyList<MergeFormatterCall> formatters,
        CultureInfo culture,
        TemplateOutputMode outputMode
    )
    {
        if (formatters.Count == 0)
        {
            return Plain(ConvertToText(initialValue, culture));
        }

        var currentObj = initialValue;
        var currentFormatted = Plain(string.Empty);

        foreach (var call in formatters)
        {
            var handler = _handlers[call.Name];
            currentFormatted = handler(currentObj, call.Argument, culture, outputMode);
            currentObj = currentFormatted.Text;
        }

        return currentFormatted;
    }

    public static string ConvertToText(object? value, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            IFormattable formattable => formattable.ToString(format: null, culture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static FormattedMergeValue Plain(string text)
    {
        return new FormattedMergeValue(text, AlreadyHtmlEncoded: false);
    }

    private static FormattedMergeValue ApplyDefault(
        object? value,
        string? arg,
        CultureInfo culture,
        TemplateOutputMode _
    )
    {
        var text = ConvertToText(value, culture);
        return Plain(string.IsNullOrWhiteSpace(text) ? arg ?? string.Empty : text);
    }

    private static FormattedMergeValue ApplyUpper(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        return Plain(ConvertToText(value, culture).ToUpper(culture));
    }

    private static FormattedMergeValue ApplyLower(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        return Plain(ConvertToText(value, culture).ToLower(culture));
    }

    private static FormattedMergeValue ApplyTitle(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        var text = ConvertToText(value, culture);
        return Plain(culture.TextInfo.ToTitleCase(text.ToLower(culture)));
    }

    private static FormattedMergeValue ApplyTrim(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        return Plain(ConvertToText(value, culture).Trim());
    }

    private static FormattedMergeValue ApplyTruncate(
        object? value,
        string? arg,
        CultureInfo culture,
        TemplateOutputMode _
    )
    {
        var text = ConvertToText(value, culture);
        return
            !int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxLen)
            || maxLen <= 0
            || text.Length <= maxLen
            ? Plain(text)
            : Plain(string.Concat(text.AsSpan(0, maxLen), "…"));
    }

    private static FormattedMergeValue ApplyDate(
        object? value,
        string? arg,
        CultureInfo culture,
        TemplateOutputMode _
    )
    {
        var format = string.IsNullOrWhiteSpace(arg) ? "d" : arg;
        return value switch
        {
            DateTimeOffset dto => Plain(dto.ToString(format, culture)),
            DateOnly dateOnly => Plain(dateOnly.ToString(format, culture)),
            DateTime dt => Plain(dt.ToString(format, culture)),
            string s
                when DateTimeOffset.TryParse(s, culture, DateTimeStyles.None, out var parsed) =>
                Plain(parsed.ToString(format, culture)),
            _ => Plain(ConvertToText(value, culture)),
        };
    }

    private static FormattedMergeValue ApplyCurrency(
        object? value,
        string? arg,
        CultureInfo culture,
        TemplateOutputMode _
    )
    {
        return !TryConvertToDecimal(value, culture, out var amount)
                ? Plain(ConvertToText(value, culture))
            : string.IsNullOrWhiteSpace(arg) ? Plain(amount.ToString("C", culture))
            : Plain(
                $"{arg.Trim().ToUpper(CultureInfo.InvariantCulture)} {amount.ToString("N2", culture)}"
            );
    }

    private static FormattedMergeValue ApplyNumber(
        object? value,
        string? arg,
        CultureInfo culture,
        TemplateOutputMode _
    )
    {
        var format = string.IsNullOrWhiteSpace(arg) ? "N0" : arg;
        return TryConvertToDecimal(value, culture, out var number)
            ? Plain(number.ToString(format, culture))
            : Plain(ConvertToText(value, culture));
    }

    private static FormattedMergeValue ApplyPhone(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        var raw = ConvertToText(value, culture);
        var digits = ExtractDigits(raw);
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        return digits.Length != 10
            ? Plain(raw)
            : Plain($"({digits[..3]}) {digits[3..6]}-{digits[6..]}");
    }

    private static FormattedMergeValue ApplyUrlEncode(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode __
    )
    {
        return Plain(Uri.EscapeDataString(ConvertToText(value, culture)));
    }

    private static FormattedMergeValue ApplyNewlinesToBr(
        object? value,
        string? _,
        CultureInfo culture,
        TemplateOutputMode outputMode
    )
    {
        var text = ConvertToText(value, culture);
        if (outputMode == TemplateOutputMode.PlainText)
        {
            return Plain(text);
        }

        var encoded = HtmlTextEncoder.Encode(text);
        var withBreaks = encoded
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
        return new FormattedMergeValue(withBreaks, AlreadyHtmlEncoded: true);
    }

    private static bool TryConvertToDecimal(object? value, CultureInfo culture, out decimal result)
    {
        switch (value)
        {
            case decimal d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case double dbl:
                result = Convert.ToDecimal(dbl);
                return true;
            case float flt:
                result = Convert.ToDecimal(flt);
                return true;
            case string s:
                return decimal.TryParse(s, NumberStyles.Number, culture, out result);
            default:
                result = 0m;
                return false;
        }
    }

    private static string ExtractDigits(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsAsciiDigit(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
