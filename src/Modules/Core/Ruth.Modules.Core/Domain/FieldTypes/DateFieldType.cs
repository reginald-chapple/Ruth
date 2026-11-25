using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class DateFieldType : FieldTypePluginBase
{
    public override string Key => "date";
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
            0x4
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e004 */
    ;
    public override string Name => "Date";
    public override string Description => "Calendar date stored in ISO yyyy-MM-dd format.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Date;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [new("format", "Display Format", "Date format string.", "yyyy-MM-dd")];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return DateOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
        )
            ? Result.Success<string?>(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            : ValidationError("Invalid", "Value must be a valid calendar date (yyyy-MM-dd).");
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (
            !DateOnly.TryParse(
                storedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date
            )
        )
        {
            return storedValue ?? string.Empty;
        }

        var format = GetQualifier(qualifiers, "format") ?? "yyyy-MM-dd";
        return date.ToString(format, CultureInfo.InvariantCulture);
    }
}
