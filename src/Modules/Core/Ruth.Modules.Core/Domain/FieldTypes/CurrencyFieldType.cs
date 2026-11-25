using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class CurrencyFieldType : FieldTypePluginBase
{
    public override string Key => "currency";
    public override Guid SystemGuid =>
        new(
            0xa75dfcd5,
            0x277,
            0x44a4,
            0x90,
            0x7e,
            0x7b,
            0x9d,
            0x4c,
            0x25,
            0xe0,
            0x3
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e003 */
    ;
    public override string Name => "Currency";
    public override string Description => "Monetary amount normalized to two decimal places.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Currency;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("currencySymbol", "Currency Symbol", "Symbol prefixed when formatting.", "$"),
            new("minValue", "Minimum Amount", "Inclusive minimum amount."),
            new("maxValue", "Maximum Amount", "Inclusive maximum amount."),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var symbol = GetQualifier(qualifiers, "currencySymbol") ?? "$";
        var cleaned = value.Replace(symbol, string.Empty, StringComparison.Ordinal).Trim();
        if (
            !decimal.TryParse(
                cleaned,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed
            )
        )
        {
            return ValidationError("Invalid", "Value must be a valid currency amount.");
        }

        var min = GetDecimalQualifier(qualifiers, "minValue");
        if (min.HasValue && parsed < min.Value)
        {
            return ValidationError(
                "Min",
                string.Create(CultureInfo.InvariantCulture, $"Amount must be at least {min.Value}.")
            );
        }

        var max = GetDecimalQualifier(qualifiers, "maxValue");
        if (max.HasValue && parsed > max.Value)
        {
            return ValidationError(
                "Max",
                string.Create(CultureInfo.InvariantCulture, $"Amount cannot exceed {max.Value}.")
            );
        }

        var normalized = Math.Round(parsed, decimals: 2, MidpointRounding.AwayFromZero);
        return Result.Success<string?>(normalized.ToString("F2", CultureInfo.InvariantCulture));
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (
            !decimal.TryParse(
                storedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed
            )
        )
        {
            return storedValue ?? string.Empty;
        }

        var symbol = GetQualifier(qualifiers, "currencySymbol") ?? "$";
        return $"{symbol}{parsed.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
