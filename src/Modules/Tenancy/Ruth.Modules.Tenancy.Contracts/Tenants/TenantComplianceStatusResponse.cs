using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Detailed compliance, IRS verification, Stripe Connect, and custom domain status for a tenant.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Slug">The immutable workspace slug.</param>
/// <param name="LegalName">The IRS-registered legal organization name.</param>
/// <param name="DisplayName">The public/workspace display name.</param>
/// <param name="Ein">The formatted Employer Identification Number (XX-XXXXXXX).</param>
/// <param name="IrsSubsectionCode">The IRS tax-exempt subsection code (for example 501(c)(3)).</param>
/// <param name="StateCode">The two-letter US state or territory code of incorporation.</param>
/// <param name="TimeZoneId">The organization's IANA time zone identifier.</param>
/// <param name="LifecycleStage">The current SaaS lifecycle stage.</param>
/// <param name="IrsStatus">The latest IRS EO BMF verification status.</param>
/// <param name="IrsOfficialName">The official organization name returned by IRS EO BMF, if matched.</param>
/// <param name="IrsDeductibilityCode">The IRS Pub 78 deductibility classification (for example PC or PF).</param>
/// <param name="IrsVerifiedOnUtc">The UTC timestamp of the latest IRS check.</param>
/// <param name="StripeConnectAccountId">The connected Stripe Express account ID, if linked.</param>
/// <param name="StripeChargesEnabled">Whether Stripe Connect card/ACH charges are enabled.</param>
/// <param name="StripePayoutsEnabled">Whether Stripe Connect payouts are enabled.</param>
/// <param name="StripeMockMode">Whether Stripe Connect is running in deterministic mock/demo mode.</param>
/// <param name="SubmittedForReviewOnUtc">The UTC timestamp when the tenant submitted its go-live request.</param>
/// <param name="ApprovedOnUtc">The UTC timestamp when a platform operator approved go-live.</param>
/// <param name="ApprovedByOperatorEmail">The operator email that approved go-live.</param>
/// <param name="ReviewNotes">Operator review notes or rejection reason.</param>
/// <param name="Domains">The sandbox and custom domains registered for this tenant.</param>
public sealed record TenantComplianceStatusResponse(
    Guid TenantId,
    string Slug,
    string LegalName,
    string DisplayName,
    string Ein,
    string IrsSubsectionCode,
    string StateCode,
    string TimeZoneId,
    TenantLifecycleStage LifecycleStage,
    IrsVerificationStatus IrsStatus,
    string? IrsOfficialName,
    string? IrsDeductibilityCode,
    DateTimeOffset? IrsVerifiedOnUtc,
    string? StripeConnectAccountId,
    bool StripeChargesEnabled,
    bool StripePayoutsEnabled,
    bool StripeMockMode,
    DateTimeOffset? SubmittedForReviewOnUtc,
    DateTimeOffset? ApprovedOnUtc,
    string? ApprovedByOperatorEmail,
    string? ReviewNotes,
    IReadOnlyList<TenantDomainResponse> Domains
);
