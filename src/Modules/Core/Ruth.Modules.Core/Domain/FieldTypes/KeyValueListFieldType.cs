using System.Text.Json;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class KeyValueListFieldType : FieldTypePluginBase
{
    public override string Key => "keyvaluelist";
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
            0x14
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e014 */
    ;
    public override string Name => "Key Value List";
    public override string Description =>
        "Ordered list of key/value pairs stored as JSON object or array.";
    public override FieldEditorKind EditorKind => FieldEditorKind.KeyValueList;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("keyPrompt", "Key Prompt", "Placeholder label for keys.", "Key"),
            new("valuePrompt", "Value Prompt", "Placeholder label for values.", "Value"),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                ? Result.Success<string?>(JsonSerializer.Serialize(doc.RootElement))
                : ValidationError("Invalid", "Key value list must be a JSON object or array.");
        }
        catch (JsonException)
        {
            return ValidationError("InvalidJson", "Key value list must be valid JSON.");
        }
    }
}
