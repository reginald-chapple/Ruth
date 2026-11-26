namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Represents a newly created or resumed Stripe Connect Express onboarding session.</summary>
/// <param name="AccountId">The Stripe connected account identifier.</param>
/// <param name="OnboardingUrl">The onboarding redirect URL.</param>
/// <param name="ExpiresAtUtc">The UTC expiration timestamp of the onboarding link.</param>
public sealed record StripeConnectSessionResult(
    string AccountId,
    string OnboardingUrl,
    DateTimeOffset ExpiresAtUtc
);
