using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Creates or resumes a Stripe Connect Express onboarding session for the active tenant.</summary>
/// <param name="ReturnUrl">The tenant settings URL to redirect to after Stripe Connect onboarding.</param>
/// <param name="RefreshUrl">The tenant settings URL to redirect to when the onboarding link expires.</param>
public sealed record StartStripeConnectOnboardingCommand(string ReturnUrl, string RefreshUrl)
    : ICommand<StripeConnectOnboardingResponse>;
