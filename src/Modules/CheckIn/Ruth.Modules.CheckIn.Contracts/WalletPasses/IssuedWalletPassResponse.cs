namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Read model representing an issued smart wallet pass.</summary>
/// <param name="Id">Unique issued pass identifier.</param>
/// <param name="TemplateId">Parent template identifier.</param>
/// <param name="TemplateName">Parent template display name.</param>
/// <param name="PersonId">Holder CRM person identifier.</param>
/// <param name="SerialNumber">Unique PassKit and Google Wallet serial number.</param>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier.</param>
/// <param name="ScanToken">Collision-free Kiosk barcode scan token (prefixed WLT-).</param>
/// <param name="CardStyle">Semantic card style.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="Revision">Monotonic update revision counter.</param>
/// <param name="UpdatedTag">Monotonic sequence tag used by PassKit passesUpdatedSince queries.</param>
/// <param name="RegisteredDeviceCount">Number of Apple Wallet devices registered for push updates.</param>
/// <param name="DynamicFields">Current live cardholder, tier, balance, shift, and event fields.</param>
/// <param name="IssuedAtUtc">UTC timestamp when issued.</param>
/// <param name="LastUpdatedAtUtc">UTC timestamp of most recent field or status update.</param>
/// <param name="ExpiresAtUtc">Optional UTC expiration timestamp.</param>
public sealed record IssuedWalletPassResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    Guid PersonId,
    string SerialNumber,
    string PassTypeIdentifier,
    string ScanToken,
    WalletPassCardStyle CardStyle,
    IssuedWalletPassStatus Status,
    int Revision,
    string UpdatedTag,
    int RegisteredDeviceCount,
    WalletPassDynamicFieldsDto DynamicFields,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset LastUpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc
);
