using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Summary projection of a tenant for the internal Platform Operator review queue.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Slug">The workspace slug.</param>
/// <param name="LegalName">The legal organization name.</param>
/// <param name="Ein">The formatted EIN (XX-XXXXXXX).</param>
/// <param name="IrsSubsectionCode">The IRS subsection code.</param>
/// <param name="StateCode">The state of incorporation.</param>
/// <param name="LifecycleStage">The current lifecycle stage.</param>
/// <param name="IrsStatus">The latest IRS EO BMF verification status.</param>
/// <param name="IrsOfficialName">The official name matched in IRS EO BMF.</param>
/// <param name="StripeChargesEnabled">Whether Stripe Connect charges are enabled.</param>
/// <param name="StripePayoutsEnabled">Whether Stripe Connect payouts are enabled.</param>
/// <param name="PrimaryHostname">The active primary hostname.</param>
/// <param name="CreatedOnUtc">The UTC signup timestamp.</param>
/// <param name="SubmittedForReviewOnUtc">The UTC go-live submission timestamp.</param>
public sealed record TenantSummaryResponse(
    Guid TenantId,
    string Slug,
    string LegalName,
    string Ein,
    string IrsSubsectionCode,
    string StateCode,
    TenantLifecycleStage LifecycleStage,
    IrsVerificationStatus IrsStatus,
    string? IrsOfficialName,
    bool StripeChargesEnabled,
    bool StripePayoutsEnabled,
    string PrimaryHostname,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? SubmittedForReviewOnUtc
);
