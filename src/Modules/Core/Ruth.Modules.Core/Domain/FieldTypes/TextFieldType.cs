using System.Globalization;
using System.Text.RegularExpressions;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class TextFieldType : FieldTypePluginBase
{
    public override string Key => "text";
    public override Guid SystemGuid =>
        new(
            0x9c204cd0,
            0x1233,
            0x41c5,
            0x81,
            0x8a,
            0xc5,
            0xda,
            0x43,
            0x94,
            0x45,
            0xaa
        ) /* 9c204cd0-1233-41c5-818a-c5da439445aa */
    ;
    public override string Name => "Text";
    public override string Description =>
        "Single-line text value with optional max length and regex validation.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Text;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new("maxLength", "Max Length", "Maximum allowed character count."),
            new("regexPattern", "Regex Pattern", "Optional regular expression pattern."),
            new("isPassword", "Password Mask", "Whether to mask displayed value.", "false"),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var maxLength = GetIntQualifier(qualifiers, "maxLength");
        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            return ValidationError(
                "MaxLength",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Text cannot exceed {maxLength.Value} characters."
                )
            );
        }

        var pattern = GetQualifier(qualifiers, "regexPattern");
        return
            pattern is not null
            && !Regex.IsMatch(
                value,
                pattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1)
            )
            ? (Result<string?>)
                ValidationError("Pattern", "Text value does not match the required pattern.")
            : Result.Success<string?>(value);
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return string.IsNullOrEmpty(storedValue) ? string.Empty
            : GetBoolQualifier(qualifiers, "isPassword") ? "********"
            : storedValue;
    }
}
