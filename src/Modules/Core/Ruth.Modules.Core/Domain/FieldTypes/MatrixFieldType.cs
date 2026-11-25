using System.Globalization;
using System.Text.Json;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class MatrixFieldType : FieldTypePluginBase
{
    public override string Key => "matrix";
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
            0x22
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e022 */
    ;
    public override string Name => "Attribute Matrix";
    public override string Description =>
        "Repeatable structured rows conforming to an AttributeMatrixTemplate stored in JSON.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Matrix;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new(
                "attributeMatrixTemplateId",
                "Matrix Template ID",
                "UUID of the governing AttributeMatrixTemplate."
            ),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return ValidationError(
                    "Invalid",
                    "Matrix value must be a JSON array of row objects."
                );
            }

            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                {
                    return ValidationError("InvalidRow", "Each matrix row must be a JSON object.");
                }
            }

            return Result.Success<string?>(JsonSerializer.Serialize(doc.RootElement));
        }
        catch (JsonException)
        {
            return ValidationError("InvalidJson", "Matrix value must be valid JSON.");
        }
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return "0 rows";
        }

        try
        {
            using var doc = JsonDocument.Parse(storedValue);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var count = doc.RootElement.GetArrayLength();
                return count == 1
                    ? "1 row"
                    : string.Create(CultureInfo.InvariantCulture, $"{count} rows");
            }
        }
        catch (JsonException)
        {
            return storedValue;
        }

        return storedValue;
    }
}
