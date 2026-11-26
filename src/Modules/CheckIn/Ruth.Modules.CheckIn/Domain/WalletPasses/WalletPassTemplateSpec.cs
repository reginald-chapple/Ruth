using Ruth.Modules.CheckIn.Contracts.WalletPasses;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Specification for creating or updating a <see cref="WalletPassTemplate" />.</summary>
public sealed record WalletPassTemplateSpec(
    string Name,
    string Key,
    string PassTypeIdentifier,
    string OrganizationName,
    string Description,
    WalletPassCardStyle CardStyle = WalletPassCardStyle.MemberCredential,
    string BackgroundColorHex = "#1E3A8A",
    string ForegroundColorHex = "#FFFFFF",
    string LabelColorHex = "#BFDBFE",
    string? LogoText = null,
    int? DefaultValidityDays = null,
    bool IsActive = true,
    bool IsSystem = false,
    Guid? SystemGuid = null
);
