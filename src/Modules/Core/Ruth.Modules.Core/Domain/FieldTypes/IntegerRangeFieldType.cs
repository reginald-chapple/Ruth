using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class IntegerRangeFieldType : FieldTypePluginBase
{
    public override string Key => "integerrange";
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
            0x8
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e008 */
    ;
    public override string Name => "Integer Range";
    public override string Description => "Lower and upper whole number pair stored as min|max.";
    public override FieldEditorKind EditorKind => FieldEditorKind.IntegerRange;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var parts = value.Split('|', count: 2);
        if (parts.Length != 2)
        {
            return ValidationError("Invalid", "Integer range must be formatted as min|max.");
        }

        int? min = null;
        int? max = null;
        if (!string.IsNullOrWhiteSpace(parts[0]))
        {
            if (
                !int.TryParse(
                    parts[0].Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedMin
                )
            )
            {
                return ValidationError("InvalidMin", "Lower integer bound is invalid.");
            }

            min = parsedMin;
        }

        if (!string.IsNullOrWhiteSpace(parts[1]))
        {
            if (
                !int.TryParse(
                    parts[1].Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedMax
                )
            )
            {
                return ValidationError("InvalidMax", "Upper integer bound is invalid.");
            }

            max = parsedMax;
        }

        return min.HasValue && max.HasValue && min.Value > max.Value
            ? (Result<string?>)ValidationError("Order", "Lower bound cannot exceed upper bound.")
            : Result.Success<string?>(
                $"{min?.ToString(CultureInfo.InvariantCulture)}|{max?.ToString(CultureInfo.InvariantCulture)}"
            );
    }
}
