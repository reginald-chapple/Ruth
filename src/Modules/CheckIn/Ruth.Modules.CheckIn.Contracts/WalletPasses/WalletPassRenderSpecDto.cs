namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Complete normalized rendering input supplied to an <see cref="IWalletPassProvider" />.</summary>
/// <param name="IssuedPassId">Identifier of the issued wallet pass.</param>
/// <param name="SerialNumber">Collision-free PassKit/Google serial number.</param>
/// <param name="ScanToken">Sub-50ms Kiosk QR/Code-128 scan payload token (prefixed WLT-).</param>
/// <param name="PassTypeIdentifier">Apple PassKit passTypeIdentifier or Google issuer class suffix.</param>
/// <param name="OrganizationName">Organization name displayed in the pass header.</param>
/// <param name="TemplateName">Template title.</param>
/// <param name="Description">Accessible pass description required by Apple PassKit.</param>
/// <param name="CardStyle">Visual and semantic pass style.</param>
/// <param name="BackgroundColorHex">CSS hex background color (#RRGGBB).</param>
/// <param name="ForegroundColorHex">CSS hex primary value text color (#RRGGBB).</param>
/// <param name="LabelColorHex">CSS hex field label text color (#RRGGBB).</param>
/// <param name="LogoText">Header logo text printed next to the pass icon.</param>
/// <param name="WebServiceBaseUrl">Optional HTTPS base URL for Apple PassKit v1 device callbacks.</param>
/// <param name="AppleAuthenticationToken">Derived 64-character authentication token embedded in pass.json.</param>
/// <param name="IssuedAtUtc">UTC timestamp when the pass was issued.</param>
/// <param name="ExpiresAtUtc">Optional UTC expiration timestamp.</param>
/// <param name="Revision">Monotonic pass revision counter.</param>
/// <param name="DynamicFields">Live cardholder and balance/shift/event fields.</param>
public sealed record WalletPassRenderSpecDto(
    Guid IssuedPassId,
    string SerialNumber,
    string ScanToken,
    string PassTypeIdentifier,
    string OrganizationName,
    string TemplateName,
    string Description,
    WalletPassCardStyle CardStyle,
    string BackgroundColorHex,
    string ForegroundColorHex,
    string LabelColorHex,
    string? LogoText,
    string? WebServiceBaseUrl,
    string? AppleAuthenticationToken,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    int Revision,
    WalletPassDynamicFieldsDto DynamicFields
);
