namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>Result payload returned after one-time initial onboarding completes.</summary>
/// <param name="OrganizationId">The UUIDv7 identifier of the created organization profile.</param>
/// <param name="AdminUserId">The UUIDv7 identifier of the created administrator user account.</param>
/// <param name="CompletedOnUtc">The UTC timestamp when onboarding completed.</param>
public sealed record CompleteOnboardingResponse(
    Guid OrganizationId,
    Guid AdminUserId,
    DateTimeOffset CompletedOnUtc
);
