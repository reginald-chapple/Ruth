using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Self-service signup command provisioning an isolated Sandbox workspace for a tax-exempt organization.</summary>
/// <param name="LegalName">The IRS-registered legal organization name.</param>
/// <param name="DisplayName">The organization's public and workspace display name.</param>
/// <param name="Slug">The desired lowercase workspace subdomain slug.</param>
/// <param name="Ein">The organization's Employer Identification Number (XX-XXXXXXX).</param>
/// <param name="IrsSubsectionCode">The IRS tax-exempt subsection code (for example 501(c)(3)).</param>
/// <param name="StateCode">The two-letter US state or territory code of incorporation.</param>
/// <param name="TimeZoneId">The organization's IANA time zone identifier.</param>
/// <param name="PrimaryBranchName">The display name of the organization's primary branch or center.</param>
/// <param name="AdminUserName">The initial organization administrator username.</param>
/// <param name="AdminEmail">The initial organization administrator email address.</param>
/// <param name="AdminDisplayName">The initial organization administrator full display name.</param>
/// <param name="AdminPassword">The initial organization administrator password.</param>
public sealed record SignupTaxExemptTenantCommand(
    string LegalName,
    string DisplayName,
    string Slug,
    string Ein,
    string IrsSubsectionCode,
    string StateCode,
    string TimeZoneId,
    string PrimaryBranchName,
    string AdminUserName,
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword
) : ICommand<SignupTaxExemptTenantResponse>, IPlatformControlPlaneRequest;
