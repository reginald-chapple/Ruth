using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class SingleSelectFieldType : FieldTypePluginBase
{
    public override string Key => "singleselect";
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
            0x10
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e010 */
    ;
    public override string Name => "Single Select";
    public override string Description => "Selection of one value from a configured option list.";
    public override FieldEditorKind EditorKind => FieldEditorKind.SingleSelect;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [new("values", "Options", "Comma or newline separated values (value or value^Label).")];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var options = ParseSelectOptions(qualifiers);
        if (options.Count == 0)
        {
            return Result.Success<string?>(value);
        }

        var (Value, Label) = options.Find(o =>
            string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase)
        );
        return string.IsNullOrEmpty(Value)
            ? ValidationError("InvalidOption", $"'{value}' is not a valid option.")
            : Result.Success<string?>(Value);
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

        var options = ParseSelectOptions(qualifiers);
        var (Value, Label) = options.Find(o =>
            string.Equals(o.Value, storedValue, StringComparison.OrdinalIgnoreCase)
        );
        return string.IsNullOrEmpty(Label) ? storedValue : Label;
    }
}
