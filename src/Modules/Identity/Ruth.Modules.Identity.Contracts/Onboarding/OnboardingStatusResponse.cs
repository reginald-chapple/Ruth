namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>Represents the current first-run onboarding and self-registration lock state.</summary>
/// <param name="IsOnboardingRequired">
/// <see langword="true" /> when initial system setup has not yet been completed.
/// </param>
/// <param name="IsSelfRegistrationLocked">
/// <see langword="true" /> when anonymous first-run self-registration is permanently locked.
/// </param>
/// <param name="OrganizationName">
/// The configured organization name after onboarding completes, or <see langword="null" /> beforehand.
/// </param>
public sealed record OnboardingStatusResponse(
    bool IsOnboardingRequired,
    bool IsSelfRegistrationLocked,
    string? OrganizationName
);
