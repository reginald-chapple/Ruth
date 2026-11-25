using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class CategoryFieldType : FieldTypePluginBase
{
    public override string Key => "category";
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
            0x13
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e013 */
    ;
    public override string Name => "Category";
    public override string Description => "Single or comma-separated Category UUID reference(s).";
    public override FieldEditorKind EditorKind => FieldEditorKind.Category;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new(
                "entityTypeKey",
                "Entity Type Key",
                "Entity type key scoping selectable categories."
            ),
            new(
                "allowMultiple",
                "Allow Multiple",
                "Whether multiple categories may be selected.",
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
            return ValidationError("SingleOnly", "Only a single Category identifier is allowed.");
        }

        var normalized = new List<string>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!Guid.TryParse(token, out var parsed) || parsed == Guid.Empty)
            {
                return ValidationError("InvalidGuid", $"'{token}' is not a valid Category UUID.");
            }

            normalized.Add(parsed.ToString("D"));
        }

        return Result.Success<string?>(string.Join(',', normalized));
    }
}
