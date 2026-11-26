using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal static class WalletPassMapper
{
    public static WalletPassTemplateResponse ToResponse(WalletPassTemplate template)
    {
        return new WalletPassTemplateResponse(
            template.Id.Value,
            template.Name,
            template.Key,
            template.PassTypeIdentifier,
            template.OrganizationName,
            template.Description,
            template.CardStyle,
            template.BackgroundColorHex,
            template.ForegroundColorHex,
            template.LabelColorHex,
            template.LogoText,
            template.DefaultValidityDays,
            template.IsSystem,
            template.IsActive,
            template.CreatedOnUtc
        );
    }

    public static IssuedWalletPassResponse ToResponse(
        IssuedWalletPass pass,
        string templateName,
        int registeredDeviceCount
    )
    {
        return new IssuedWalletPassResponse(
            pass.Id.Value,
            pass.TemplateId.Value,
            templateName,
            pass.PersonId,
            pass.SerialNumber,
            pass.PassTypeIdentifier,
            pass.ScanToken,
            pass.CardStyle,
            pass.Status,
            pass.Revision,
            pass.UpdatedTag,
            registeredDeviceCount,
            pass.DynamicFields,
            pass.IssuedAtUtc,
            pass.LastUpdatedAtUtc,
            pass.ExpiresAtUtc
        );
    }

    public static WalletPassRenderSpecDto ToRenderSpec(
        IssuedWalletPass pass,
        WalletPassTemplate template,
        string? rawAppleAuthToken,
        string? webServiceBaseUrl
    )
    {
        return new WalletPassRenderSpecDto(
            pass.Id.Value,
            pass.SerialNumber,
            pass.ScanToken,
            pass.PassTypeIdentifier,
            template.OrganizationName,
            template.Name,
            template.Description,
            pass.CardStyle,
            template.BackgroundColorHex,
            template.ForegroundColorHex,
            template.LabelColorHex,
            template.LogoText,
            webServiceBaseUrl,
            rawAppleAuthToken,
            pass.IssuedAtUtc,
            pass.ExpiresAtUtc,
            pass.Revision,
            pass.DynamicFields
        );
    }
}
