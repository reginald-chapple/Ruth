using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class TimeFieldType : FieldTypePluginBase
{
    public override string Key => "time";
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
            0x7
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e007 */
    ;
    public override string Name => "Time";
    public override string Description => "Time of day stored in 24-hour HH:mm:ss format.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Time;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return TimeOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time
        )
            ? Result.Success<string?>(time.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
            : ValidationError("Invalid", "Value must be a valid time of day (HH:mm or HH:mm:ss).");
    }
}
