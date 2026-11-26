using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Command to register a new organization, corporate sponsor, or institutional grantor in CRM and Finance.</summary>
/// <param name="Name">Organization legal or display name.</param>
/// <param name="Email">Optional primary contact or billing email.</param>
/// <param name="PhoneNumber">Optional primary contact phone number.</param>
/// <param name="PrimaryBranchId">Optional associated Branch/Site identifier.</param>
/// <param name="TaxIdentifier">Optional tax ID / EIN.</param>
/// <param name="SponsorTier">Optional sponsor tier or category.</param>
/// <param name="WebsiteUrl">Optional website URL.</param>
/// <param name="BillingNotes">Optional billing or stewardship notes.</param>
public sealed record CreateOrganizationCommand(
    string Name,
    string? Email = null,
    string? PhoneNumber = null,
    Guid? PrimaryBranchId = null,
    string? TaxIdentifier = null,
    string? SponsorTier = null,
    string? WebsiteUrl = null,
    string? BillingNotes = null
) : ICommand<OrganizationDetailResponse>;
