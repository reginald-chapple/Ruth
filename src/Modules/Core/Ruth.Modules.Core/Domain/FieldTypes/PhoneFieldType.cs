using System.Text;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class PhoneFieldType : FieldTypePluginBase
{
    public override string Key => "phone";
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
            0x20
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e020 */
    ;
    public override string Name => "Phone Number";
    public override string Description => "Telephone number normalized to E.164-friendly digits.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Phone;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var hasLeadingPlus = value.StartsWith('+');
        var digits = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
            {
                _ = digits.Append(ch);
            }
            else if (ch is not (' ' or '-' or '(' or ')' or '.' or '+'))
            {
                return ValidationError("Invalid", "Phone number contains unsupported characters.");
            }
        }

        if (digits.Length is < 7 or > 15)
        {
            return ValidationError("Length", "Phone number must contain between 7 and 15 digits.");
        }

        var normalized = hasLeadingPlus ? $"+{digits}" : digits.ToString();
        return Result.Success<string?>(normalized);
    }
}
