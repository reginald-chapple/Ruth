namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Detailed projection for an organization or corporate sponsor enriched with Finance ledger metrics.</summary>
/// <param name="OrganizationPersonId">Underlying CRM Business Person identifier.</param>
/// <param name="Name">Organization legal or display name.</param>
/// <param name="Email">Primary billing or sponsor contact email.</param>
/// <param name="PrimaryPhone">Primary formatted contact phone number.</param>
/// <param name="PrimaryBranchId">Optional primary Branch/Site identifier.</param>
/// <param name="TaxIdentifier">Optional tax identification number.</param>
/// <param name="SponsorTier">Optional sponsor tier.</param>
/// <param name="WebsiteUrl">Optional website URL.</param>
/// <param name="BillingNotes">Optional billing or grant stewardship notes.</param>
/// <param name="IsActive">Whether the organization record is active.</param>
/// <param name="ActivePledgedTotal">Total active pledged amount in primary currency.</param>
/// <param name="OpenInvoiceBalance">Total unpaid balance across open invoices.</param>
/// <param name="LifetimeSettledTotal">Total settled payments and contributions recorded.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record OrganizationDetailResponse(
    Guid OrganizationPersonId,
    string Name,
    string? Email,
    string? PrimaryPhone,
    Guid? PrimaryBranchId,
    string? TaxIdentifier,
    string? SponsorTier,
    string? WebsiteUrl,
    string? BillingNotes,
    bool IsActive,
    decimal ActivePledgedTotal,
    decimal OpenInvoiceBalance,
    decimal LifetimeSettledTotal,
    DateTimeOffset CreatedOnUtc
);
