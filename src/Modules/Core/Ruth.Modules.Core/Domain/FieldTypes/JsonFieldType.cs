using System.Text.Json;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class JsonFieldType : FieldTypePluginBase
{
    public override string Key => "json";
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
            0x15
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e015 */
    ;
    public override string Name => "JSON";
    public override string Description => "Validated JSON document payload.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Json;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            return Result.Success<string?>(JsonSerializer.Serialize(doc.RootElement));
        }
        catch (JsonException)
        {
            return ValidationError("Invalid", "Value must be well-formed JSON.");
        }
    }
}
