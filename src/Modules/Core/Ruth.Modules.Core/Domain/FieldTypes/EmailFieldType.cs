using System.Net.Mail;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class EmailFieldType : FieldTypePluginBase
{
    public override string Key => "email";
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
            0x19
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e019 */
    ;
    public override string Name => "Email";
    public override string Description => "Validated email address.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Email;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return
            MailAddress.TryCreate(value, out var parsed)
            && string.Equals(parsed.Address, value, StringComparison.OrdinalIgnoreCase)
            && value.Contains('.', StringComparison.Ordinal)
            ? Result.Success<string?>(value.ToLowerInvariant())
            : ValidationError("Invalid", "Value must be a valid email address.");
    }
}
