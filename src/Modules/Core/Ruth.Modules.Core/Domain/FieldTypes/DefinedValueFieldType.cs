using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class DefinedValueFieldType : FieldTypePluginBase
{
    public override string Key => "definedvalue";
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
            0x12
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e012 */
    ;
    public override string Name => "Defined Value";
    public override string Description =>
        "Single or comma-separated DefinedValue UUID reference(s).";
    public override FieldEditorKind EditorKind => FieldEditorKind.DefinedValue;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("definedTypeId", "Defined Type ID", "UUID of the source DefinedType."),
            new(
                "allowMultiple",
                "Allow Multiple",
                "Whether multiple values may be selected.",
                "false"
            ),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var allowMultiple = GetBoolQualifier(qualifiers, "allowMultiple");
        var tokens = value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (!allowMultiple && tokens.Length > 1)
        {
            return ValidationError(
                "SingleOnly",
                "Only a single DefinedValue identifier is allowed."
            );
        }

        var normalized = new List<string>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!Guid.TryParse(token, out var parsed) || parsed == Guid.Empty)
            {
                return ValidationError(
                    "InvalidGuid",
                    $"'{token}' is not a valid DefinedValue UUID."
                );
            }

            normalized.Add(parsed.ToString("D"));
        }

        return Result.Success<string?>(string.Join(',', normalized));
    }
}
