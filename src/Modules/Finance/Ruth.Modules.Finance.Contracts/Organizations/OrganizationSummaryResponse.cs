namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Summary projection for an organization, corporate sponsor, or institutional grantor.</summary>
/// <param name="OrganizationPersonId">Underlying CRM Business Person identifier.</param>
/// <param name="Name">Organization legal or display name.</param>
/// <param name="Email">Primary billing or sponsor contact email.</param>
/// <param name="PrimaryPhone">Primary formatted contact phone number.</param>
/// <param name="PrimaryBranchId">Optional primary Branch/Site identifier.</param>
/// <param name="TaxIdentifier">Optional employer/tax identification number (for example, EIN/VAT).</param>
/// <param name="SponsorTier">Optional sponsor or institutional tier classification.</param>
/// <param name="WebsiteUrl">Optional organization website URL.</param>
/// <param name="IsActive">Whether the organization record is active.</param>
public sealed record OrganizationSummaryResponse(
    Guid OrganizationPersonId,
    string Name,
    string? Email,
    string? PrimaryPhone,
    Guid? PrimaryBranchId,
    string? TaxIdentifier,
    string? SponsorTier,
    string? WebsiteUrl,
    bool IsActive
);
