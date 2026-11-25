using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class RatingFieldType : FieldTypePluginBase
{
    public override string Key => "rating";
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
            0x21
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e021 */
    ;
    public override string Name => "Rating";
    public override string Description => "Numeric rating bounded from 0 to maxRating (default 5).";
    public override FieldEditorKind EditorKind => FieldEditorKind.Rating;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("maxRating", "Max Rating", "Highest allowed rating score.", "5"),
            new("allowHalf", "Allow Half Stars", "Allow 0.5 increments when true.", "false"),
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
                out var rating
            )
        )
        {
            return ValidationError("Invalid", "Rating must be numeric.");
        }

        var maxRating = Math.Max(1, GetIntQualifier(qualifiers, "maxRating") ?? 5);
        var allowHalf = GetBoolQualifier(qualifiers, "allowHalf");
        if (rating < 0 || rating > maxRating)
        {
            return ValidationError(
                "Range",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Rating must be between 0 and {maxRating}."
                )
            );
        }

        var stepCheck = allowHalf ? rating * 2m : rating;
        return stepCheck != Math.Truncate(stepCheck)
            ? (Result<string?>)
                ValidationError(
                    "Step",
                    allowHalf
                        ? "Rating must be in 0.5 increments."
                        : "Rating must be a whole number."
                )
            : Result.Success<string?>(rating.ToString(CultureInfo.InvariantCulture));
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return string.Empty;
        }

        var maxRating = Math.Max(1, GetIntQualifier(qualifiers, "maxRating") ?? 5);
        return string.Create(CultureInfo.InvariantCulture, $"{storedValue} / {maxRating}");
    }
}
