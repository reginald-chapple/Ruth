using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class MemoFieldType : FieldTypePluginBase
{
    public override string Key => "memo";
    public override Guid SystemGuid =>
        new(
            0xaa204cd0,
            0x1233,
            0x41c5,
            0x81,
            0x8a,
            0xc5,
            0xda,
            0x43,
            0x94,
            0x45,
            0xab
        ) /* aa204cd0-1233-41c5-818a-c5da439445ab */
    ;
    public override string Name => "Memo";
    public override string Description => "Multi-line text value.";
    public override FieldEditorKind EditorKind => FieldEditorKind.MultilineText;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("numberOfRows", "Rows", "Visible rows in multiline editor.", "4"),
            new("maxLength", "Max Length", "Maximum allowed character count."),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var maxLength = GetIntQualifier(qualifiers, "maxLength");
        return maxLength.HasValue && value.Length > maxLength.Value
            ? ValidationError(
                "MaxLength",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Memo cannot exceed {maxLength.Value} characters."
                )
            )
            : Result.Success<string?>(value);
    }
}
