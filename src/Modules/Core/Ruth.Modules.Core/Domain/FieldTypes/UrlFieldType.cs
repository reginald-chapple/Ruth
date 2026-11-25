using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class UrlFieldType : FieldTypePluginBase
{
    public override string Key => "url";
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
            0x18
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e018 */
    ;
    public override string Name => "URL";
    public override string Description => "Absolute HTTP or HTTPS web address.";
    public override FieldEditorKind EditorKind => FieldEditorKind.Url;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [new("requireHttps", "Require HTTPS", "Reject non-HTTPS URLs when true.", "false")];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return ValidationError("Invalid", "Value must be a valid absolute URL.");
        }

        var requireHttps = GetBoolQualifier(qualifiers, "requireHttps");
        var isHttps = string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase
        );
        var isHttp = string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttp,
            StringComparison.OrdinalIgnoreCase
        );

        return requireHttps && !isHttps
                ? (Result<string?>)
                    ValidationError("HttpsRequired", "URL must use the HTTPS scheme.")
            : isHttp || isHttps ? Result.Success<string?>(uri.AbsoluteUri)
            : ValidationError("InvalidScheme", "URL must use HTTP or HTTPS.");
    }
}
