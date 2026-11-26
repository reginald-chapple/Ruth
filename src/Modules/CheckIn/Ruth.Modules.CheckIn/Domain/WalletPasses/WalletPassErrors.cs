using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Domain error catalog for smart wallet pass templates, issued passes, and PassKit callbacks.</summary>
public static class WalletPassErrors
{
    /// <summary>Template key slug is invalid.</summary>
    public static readonly DomainError TemplateKeyInvalid = DomainError.Validation(
        "WalletPass.TemplateKeyInvalid",
        "Wallet pass template key must be 3..80 lowercase letters, digits, dots, underscores, or hyphens."
    );

    /// <summary>Duplicate template key.</summary>
    public static readonly DomainError TemplateDuplicateKey = DomainError.Conflict(
        "WalletPass.TemplateDuplicateKey",
        "A wallet pass template with the same key already exists."
    );

    /// <summary>Template name is invalid.</summary>
    public static readonly DomainError TemplateNameInvalid = DomainError.Validation(
        "WalletPass.TemplateNameInvalid",
        "Wallet pass template name is required and must not exceed 120 characters."
    );

    /// <summary>PassTypeIdentifier is invalid.</summary>
    public static readonly DomainError PassTypeIdentifierInvalid = DomainError.Validation(
        "WalletPass.PassTypeIdentifierInvalid",
        "PassTypeIdentifier must follow reverse-DNS notation starting with pass. (for example pass.org.ruth.member)."
    );

    /// <summary>Organization name or description is invalid.</summary>
    public static readonly DomainError OrganizationOrDescriptionInvalid = DomainError.Validation(
        "WalletPass.OrganizationOrDescriptionInvalid",
        "OrganizationName (1..120 chars) and Description (1..300 chars) are required."
    );

    /// <summary>CSS hex color is invalid.</summary>
    public static readonly DomainError ColorHexInvalid = DomainError.Validation(
        "WalletPass.ColorHexInvalid",
        "Background, foreground, and label colors must be 6-digit hex strings formatted as #RRGGBB."
    );

    /// <summary>Default validity window is invalid.</summary>
    public static readonly DomainError ValidityDaysInvalid = DomainError.Validation(
        "WalletPass.ValidityDaysInvalid",
        "DefaultValidityDays must be between 1 and 3650 days when specified."
    );

    /// <summary>Template was not found.</summary>
    public static readonly DomainError TemplateNotFound = DomainError.NotFound(
        "WalletPass.TemplateNotFound",
        "The requested wallet pass template was not found."
    );

    /// <summary>Template is inactive.</summary>
    public static readonly DomainError TemplateInactive = DomainError.Validation(
        "WalletPass.TemplateInactive",
        "Cannot issue a wallet pass from an inactive template."
    );

    /// <summary>Built-in system template cannot be deleted.</summary>
    public static readonly DomainError SystemTemplateProtected = DomainError.Conflict(
        "WalletPass.SystemTemplateProtected",
        "Built-in system wallet pass templates cannot be deleted."
    );

    /// <summary>Template with issued passes cannot be deleted.</summary>
    public static readonly DomainError TemplateHasIssuedPasses = DomainError.Conflict(
        "WalletPass.TemplateHasIssuedPasses",
        "Cannot delete a wallet pass template that already has issued passes."
    );

    /// <summary>Issued pass was not found.</summary>
    public static readonly DomainError PassNotFound = DomainError.NotFound(
        "WalletPass.PassNotFound",
        "The requested smart wallet pass was not found."
    );

    /// <summary>Claim token is invalid or unrecognized.</summary>
    public static readonly DomainError ClaimTokenInvalid = DomainError.NotFound(
        "WalletPass.ClaimTokenInvalid",
        "The wallet pass claim link is invalid or was not found."
    );

    /// <summary>ApplePass authorization token is missing or invalid.</summary>
    public static readonly DomainError PassKitUnauthorized = DomainError.Unauthorized(
        "WalletPass.PassKitUnauthorized",
        "A valid Authorization: ApplePass token is required."
    );

    /// <summary>Push token is required for PassKit device registration.</summary>
    public static readonly DomainError PushTokenRequired = DomainError.Validation(
        "WalletPass.PushTokenRequired",
        "A non-empty APNs pushToken is required to register a PassKit device."
    );

    /// <summary>Issued pass is revoked or expired and cannot be used.</summary>
    public static readonly DomainError PassInactiveOrExpired = DomainError.Conflict(
        "WalletPass.PassInactiveOrExpired",
        "This wallet pass has been revoked or has expired."
    );

    /// <summary>Dynamic fields failed validation.</summary>
    public static readonly DomainError DynamicFieldsInvalid = DomainError.Validation(
        "WalletPass.DynamicFieldsInvalid",
        "Dynamic pass fields must have non-negative balances and at most 12 valid custom fields."
    );

    /// <summary>Provider signing credentials are not configured.</summary>
    public static readonly DomainError ProviderNotConfigured = DomainError.Validation(
        "WalletPass.ProviderNotConfigured",
        "Signing credentials are not configured for the requested wallet pass provider."
    );
}
