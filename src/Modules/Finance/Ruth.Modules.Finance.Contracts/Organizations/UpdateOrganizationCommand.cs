using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Command to update an existing organization or corporate sponsor record.</summary>
/// <param name="OrganizationPersonId">Target CRM Business Person identifier.</param>
/// <param name="Name">Updated organization name.</param>
/// <param name="Email">Updated contact or billing email.</param>
/// <param name="PrimaryBranchId">Updated Branch/Site identifier.</param>
/// <param name="TaxIdentifier">Updated tax ID / EIN.</param>
/// <param name="SponsorTier">Updated sponsor tier.</param>
/// <param name="WebsiteUrl">Updated website URL.</param>
/// <param name="BillingNotes">Updated billing notes.</param>
/// <param name="IsActive">Whether the organization remains active.</param>
public sealed record UpdateOrganizationCommand(
    Guid OrganizationPersonId,
    string Name,
    string? Email = null,
    Guid? PrimaryBranchId = null,
    string? TaxIdentifier = null,
    string? SponsorTier = null,
    string? WebsiteUrl = null,
    string? BillingNotes = null,
    bool IsActive = true
) : ICommand<OrganizationDetailResponse>;
