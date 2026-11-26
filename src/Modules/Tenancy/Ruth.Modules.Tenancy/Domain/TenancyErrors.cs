using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Domain;

internal static class TenancyErrors
{
    public static readonly DomainError InvalidSlug = DomainError.Validation(
        "Tenancy.InvalidSlug",
        "Workspace slug must be 3 to 50 lowercase alphanumeric characters or hyphens and cannot use a reserved platform name."
    );

    public static readonly DomainError SlugAlreadyTaken = DomainError.Conflict(
        "Tenancy.SlugAlreadyTaken",
        "The requested workspace slug or hostname is already registered."
    );

    public static readonly DomainError EinAlreadyRegistered = DomainError.Conflict(
        "Tenancy.EinAlreadyRegistered",
        "An organization with this Employer Identification Number is already registered."
    );

    public static readonly DomainError InvalidEin = DomainError.Validation(
        "Tenancy.InvalidEin",
        "Employer Identification Number must be a valid 9-digit IRS EIN (XX-XXXXXXX)."
    );

    public static readonly DomainError IneligibleTaxExemptSubsection = DomainError.Validation(
        "Tenancy.IneligibleSubsection",
        "Only IRS-recognized tax-exempt subsections (such as 501(c)(3)) are eligible for onboarding."
    );

    public static readonly DomainError InvalidStateCode = DomainError.Validation(
        "Tenancy.InvalidStateCode",
        "State code must be a valid two-letter US state, district, or territory code."
    );

    public static readonly DomainError InvalidHostname = DomainError.Validation(
        "Tenancy.InvalidHostname",
        "Custom domain must be a valid DNS hostname owned by your organization."
    );

    public static readonly DomainError DomainAlreadyRegistered = DomainError.Conflict(
        "Tenancy.DomainAlreadyRegistered",
        "This hostname is already registered."
    );

    public static readonly DomainError DomainNotFound = DomainError.NotFound(
        "Tenancy.DomainNotFound",
        "The requested custom domain record was not found."
    );

    public static readonly DomainError DomainVerificationMismatch = DomainError.Validation(
        "Tenancy.DomainVerificationMismatch",
        "The observed DNS TXT verification token did not match the expected verification challenge."
    );

    public static readonly DomainError TenantNotFound = DomainError.NotFound(
        "Tenancy.TenantNotFound",
        "The organization tenant record was not found."
    );

    public static readonly DomainError StripeConnectIncomplete = DomainError.Validation(
        "Tenancy.StripeConnectIncomplete",
        "Stripe Connect Express charges and payouts must be enabled before going live."
    );

    public static readonly DomainError IrsVerificationFailed = DomainError.Validation(
        "Tenancy.IrsVerificationFailed",
        "IRS Exempt Organizations Business Master File verification did not confirm active tax-exempt status."
    );

    public static readonly DomainError MockModeDisabled = DomainError.Validation(
        "Tenancy.MockModeDisabled",
        "Simulated Stripe Connect completion is only available when mock mode is enabled."
    );

    public static readonly DomainError InvalidOperatorCredentials = DomainError.Unauthorized(
        "Tenancy.InvalidOperatorCredentials",
        "Invalid platform operator credentials."
    );
}
