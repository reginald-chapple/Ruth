using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class DateTimeFieldType : FieldTypePluginBase
{
    public override string Key => "datetime";
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
            0x5
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e005 */
    ;
    public override string Name => "Date Time";
    public override string Description => "Timestamp stored in ISO 8601 UTC format.";
    public override FieldEditorKind EditorKind => FieldEditorKind.DateTime;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var dto
        )
            ? Result.Success<string?>(
                dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            )
            : ValidationError("Invalid", "Value must be a valid ISO 8601 date and time.");
    }
}
