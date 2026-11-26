namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Represents a sandbox or custom hostname bound to a tenant.</summary>
/// <param name="Id">The domain record identifier.</param>
/// <param name="Hostname">The lowercase DNS hostname.</param>
/// <param name="IsCustomDomain">Whether this is an organization-owned custom domain rather than a platform sandbox subdomain.</param>
/// <param name="IsVerified">Whether DNS TXT ownership verification has succeeded.</param>
/// <param name="IsPrimary">Whether this hostname is the canonical primary host for the tenant.</param>
/// <param name="VerificationTxtName">The DNS TXT host label required for custom domain ownership verification.</param>
/// <param name="VerificationTxtValue">The DNS TXT token value required for custom domain ownership verification.</param>
/// <param name="VerifiedOnUtc">The UTC timestamp when domain ownership was verified.</param>
public sealed record TenantDomainResponse(
    Guid Id,
    string Hostname,
    bool IsCustomDomain,
    bool IsVerified,
    bool IsPrimary,
    string? VerificationTxtName,
    string? VerificationTxtValue,
    DateTimeOffset? VerifiedOnUtc
);
