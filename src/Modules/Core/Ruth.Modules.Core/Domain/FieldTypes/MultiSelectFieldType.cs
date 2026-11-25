using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class MultiSelectFieldType : FieldTypePluginBase
{
    public override string Key => "multiselect";
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
            0x11
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e011 */
    ;
    public override string Name => "Multi Select";
    public override string Description =>
        "Comma-delimited selection of one or more configured options.";
    public override FieldEditorKind EditorKind => FieldEditorKind.MultiSelect;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [new("values", "Options", "Comma or newline separated values (value or value^Label).")];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var selected = value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (selected.Length == 0)
        {
            return Result.Success<string?>(value: null);
        }

        var options = ParseSelectOptions(qualifiers);
        var normalized = new List<string>(selected.Length);
        foreach (var item in selected.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (options.Count == 0)
            {
                normalized.Add(item);
                continue;
            }

            var (Value, Label) = options.Find(o =>
                string.Equals(o.Value, item, StringComparison.OrdinalIgnoreCase)
            );
            if (string.IsNullOrEmpty(Value))
            {
                return ValidationError("InvalidOption", $"'{item}' is not a valid option.");
            }

            normalized.Add(Value);
        }

        return Result.Success<string?>(string.Join(',', normalized));
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
        var items = storedValue.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var labels = items.Select(item =>
        {
            var (Value, Label) = options.Find(o =>
                string.Equals(o.Value, item, StringComparison.OrdinalIgnoreCase)
            );
            return string.IsNullOrEmpty(Label) ? item : Label;
        });
        return string.Join(", ", labels);
    }
}
