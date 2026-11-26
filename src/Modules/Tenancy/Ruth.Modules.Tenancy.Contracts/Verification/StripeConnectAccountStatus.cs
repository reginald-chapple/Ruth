namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Represents the KYC and capability readiness of a Stripe Connect Express account.</summary>
/// <param name="AccountId">The Stripe connected account identifier.</param>
/// <param name="ChargesEnabled">Whether live payment capture is enabled.</param>
/// <param name="PayoutsEnabled">Whether bank payouts are enabled.</param>
/// <param name="DetailsSubmitted">Whether onboarding KYC details have been submitted.</param>
public sealed record StripeConnectAccountStatus(
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted
);
