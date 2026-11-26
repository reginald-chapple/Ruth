namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Onboarding session details returned when starting Stripe Connect Express setup.</summary>
/// <param name="AccountId">The connected Stripe Express account identifier.</param>
/// <param name="OnboardingUrl">The hosted or simulated onboarding redirect URL.</param>
/// <param name="IsMockMode">Whether the platform is operating in zero-config mock Stripe Connect mode.</param>
/// <param name="ExpiresAtUtc">The UTC expiration instant of the onboarding link.</param>
public sealed record StripeConnectOnboardingResponse(
    string AccountId,
    string OnboardingUrl,
    bool IsMockMode,
    DateTimeOffset ExpiresAtUtc
);
