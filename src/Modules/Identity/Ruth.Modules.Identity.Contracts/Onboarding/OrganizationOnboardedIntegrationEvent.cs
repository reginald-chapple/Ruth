using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>Integration event raised when initial system onboarding completes.</summary>
/// <param name="OrganizationId">The identifier of the onboarded organization.</param>
/// <param name="OrganizationName">The name of the onboarded organization.</param>
/// <param name="TimeZoneId">The configured time zone identifier for the organization.</param>
/// <param name="CompletedOnUtc">The UTC timestamp when onboarding completed.</param>
public sealed record OrganizationOnboardedIntegrationEvent(
    Guid OrganizationId,
    string OrganizationName,
    string TimeZoneId,
    DateTimeOffset CompletedOnUtc
) : IntegrationEvent(CompletedOnUtc);
