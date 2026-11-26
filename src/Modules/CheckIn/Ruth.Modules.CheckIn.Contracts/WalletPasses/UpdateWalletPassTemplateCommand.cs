using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Updates an existing smart wallet pass template.</summary>
/// <param name="TemplateId">Target template identifier.</param>
/// <param name="Name">Updated template display name.</param>
/// <param name="PassTypeIdentifier">Updated PassKit passTypeIdentifier.</param>
/// <param name="OrganizationName">Updated organization header name.</param>
/// <param name="Description">Updated accessible description.</param>
/// <param name="CardStyle">Updated card style.</param>
/// <param name="BackgroundColorHex">CSS hex background color (#RRGGBB).</param>
/// <param name="ForegroundColorHex">CSS hex primary text color (#RRGGBB).</param>
/// <param name="LabelColorHex">CSS hex caption label color (#RRGGBB).</param>
/// <param name="LogoText">Optional header logo text.</param>
/// <param name="DefaultValidityDays">Optional default expiration days (1..3650).</param>
/// <param name="IsActive">Whether the template remains active.</param>
public sealed record UpdateWalletPassTemplateCommand(
    Guid TemplateId,
    string Name,
    string PassTypeIdentifier,
    string OrganizationName,
    string Description,
    WalletPassCardStyle CardStyle,
    string BackgroundColorHex,
    string ForegroundColorHex,
    string LabelColorHex,
    string? LogoText = null,
    int? DefaultValidityDays = null,
    bool IsActive = true
) : ICommand<WalletPassTemplateResponse>;
