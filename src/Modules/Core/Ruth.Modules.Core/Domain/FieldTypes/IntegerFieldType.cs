using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class IntegerFieldType : FieldTypePluginBase
{
    public override string Key => "integer";
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
            0x1
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e001 */
    ;
    public override string Name => "Integer";
    public override string Description => "32-bit whole number with optional min and max bounds.";
    public override FieldEditorKind EditorKind => FieldEditorKind.WholeNumber;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("minValue", "Minimum Value", "Inclusive minimum integer value."),
            new("maxValue", "Maximum Value", "Inclusive maximum integer value."),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (
            !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
        )
        {
            return ValidationError("Invalid", "Value must be a valid whole number.");
        }

        var min = GetIntQualifier(qualifiers, "minValue");
        if (min.HasValue && parsed < min.Value)
        {
            return ValidationError(
                "Min",
                string.Create(CultureInfo.InvariantCulture, $"Value must be at least {min.Value}.")
            );
        }

        var max = GetIntQualifier(qualifiers, "maxValue");
        return max.HasValue && parsed > max.Value
            ? (Result<string?>)
                ValidationError(
                    "Max",
                    string.Create(CultureInfo.InvariantCulture, $"Value cannot exceed {max.Value}.")
                )
            : Result.Success<string?>(parsed.ToString(CultureInfo.InvariantCulture));
    }
}
