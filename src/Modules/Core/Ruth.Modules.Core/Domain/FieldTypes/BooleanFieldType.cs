using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class BooleanFieldType : FieldTypePluginBase
{
    public override string Key => "boolean";
    public override Guid SystemGuid =>
        new(
            0x1edafded,
            0xdfe6,
            0x4334,
            0xb0,
            0x19,
            0x6e,
            0xec,
            0xba,
            0x89,
            0xe0,
            0x5a
        ) /* 1edafded-dfe6-4334-b019-6eecba89e05a */
    ;
    public override string Name => "Boolean";
    public override string Description => "True or false value.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Boolean;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("trueText", "True Text", "Display text when true.", "Yes"),
            new("falseText", "False Text", "Display text when false.", "No"),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return value.ToUpperInvariant() switch
        {
            "TRUE" or "1" or "YES" or "Y" => Result.Success<string?>("true"),
            "FALSE" or "0" or "NO" or "N" => Result.Success<string?>("false"),
            _ => ValidationError("Invalid", "Value must be a valid boolean (true or false)."),
        };
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

        var isTrue = string.Equals(storedValue, "true", StringComparison.OrdinalIgnoreCase);
        return isTrue
            ? GetQualifier(qualifiers, "trueText") ?? "Yes"
            : GetQualifier(qualifiers, "falseText") ?? "No";
    }
}
