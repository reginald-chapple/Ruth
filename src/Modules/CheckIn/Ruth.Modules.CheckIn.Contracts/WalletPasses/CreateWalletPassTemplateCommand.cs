using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Creates a smart wallet pass template for member, punch, shift, or event credentials.</summary>
/// <param name="Name">Template display name.</param>
/// <param name="Key">Unique slug key (lowercase letters, digits, dots, underscores, hyphens).</param>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier (for example pass.org.ruth.member).</param>
/// <param name="OrganizationName">Organization name rendered on the pass.</param>
/// <param name="Description">Accessible pass description.</param>
/// <param name="CardStyle">Visual and semantic card style.</param>
/// <param name="BackgroundColorHex">CSS hex background color (#RRGGBB).</param>
/// <param name="ForegroundColorHex">CSS hex primary text color (#RRGGBB).</param>
/// <param name="LabelColorHex">CSS hex caption label color (#RRGGBB).</param>
/// <param name="LogoText">Optional header logo text.</param>
/// <param name="DefaultValidityDays">Optional default expiration days (1..3650).</param>
/// <param name="IsActive">Whether the template starts active.</param>
public sealed record CreateWalletPassTemplateCommand(
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
    bool IsActive = true
) : ICommand<WalletPassTemplateResponse>;
