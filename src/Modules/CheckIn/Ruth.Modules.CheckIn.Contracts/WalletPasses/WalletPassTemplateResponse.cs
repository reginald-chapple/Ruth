namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Read model representing a smart wallet pass template.</summary>
/// <param name="Id">Unique template identifier.</param>
/// <param name="Name">Human-readable template title.</param>
/// <param name="Key">Unique URL-safe slug key.</param>
/// <param name="PassTypeIdentifier">Apple PassKit passTypeIdentifier or Google Wallet class suffix.</param>
/// <param name="OrganizationName">Organization display name printed in the pass header.</param>
/// <param name="Description">Accessible description of the pass.</param>
/// <param name="CardStyle">Semantic card style (member credential, punch card, shift, event ticket).</param>
/// <param name="BackgroundColorHex">Hex background color (#RRGGBB).</param>
/// <param name="ForegroundColorHex">Hex foreground value color (#RRGGBB).</param>
/// <param name="LabelColorHex">Hex field label color (#RRGGBB).</param>
/// <param name="LogoText">Optional header logo text.</param>
/// <param name="DefaultValidityDays">Optional default expiration window in days when issuing passes.</param>
/// <param name="IsSystem">True when protected as a built-in system template.</param>
/// <param name="IsActive">True when available for new pass issuance.</param>
/// <param name="CreatedOnUtc">Creation timestamp in UTC.</param>
public sealed record WalletPassTemplateResponse(
    Guid Id,
    string Name,
    string Key,
    string PassTypeIdentifier,
    string OrganizationName,
    string Description,
    WalletPassCardStyle CardStyle,
    string BackgroundColorHex,
    string ForegroundColorHex,
    string LabelColorHex,
    string? LogoText,
    int? DefaultValidityDays,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
