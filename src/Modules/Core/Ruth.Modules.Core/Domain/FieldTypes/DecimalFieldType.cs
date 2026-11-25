using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class DecimalFieldType : FieldTypePluginBase
{
    public override string Key => "decimal";
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
            0x2
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e002 */
    ;
    public override string Name => "Decimal";
    public override string Description => "Decimal number with optional bounds and precision.";
    public override FieldEditorKind EditorKind => FieldEditorKind.DecimalNumber;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("minValue", "Minimum Value", "Inclusive minimum decimal value."),
            new("maxValue", "Maximum Value", "Inclusive maximum decimal value."),
            new("precision", "Decimal Places", "Optional rounding scale."),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (
            !decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed
            )
        )
        {
            return ValidationError("Invalid", "Value must be a valid decimal number.");
        }

        var min = GetDecimalQualifier(qualifiers, "minValue");
        if (min.HasValue && parsed < min.Value)
        {
            return ValidationError(
                "Min",
                string.Create(CultureInfo.InvariantCulture, $"Value must be at least {min.Value}.")
            );
        }

        var max = GetDecimalQualifier(qualifiers, "maxValue");
        if (max.HasValue && parsed > max.Value)
        {
            return ValidationError(
                "Max",
                string.Create(CultureInfo.InvariantCulture, $"Value cannot exceed {max.Value}.")
            );
        }

        var precision = GetIntQualifier(qualifiers, "precision");
        if (precision.HasValue && precision.Value is >= 0 and <= 10)
        {
            parsed = Math.Round(parsed, precision.Value, MidpointRounding.AwayFromZero);
        }

        return Result.Success<string?>(parsed.ToString(CultureInfo.InvariantCulture));
    }
}
