using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>
/// Command to complete one-time initial organization and administrator setup and permanently lock self-registration.
/// </summary>
/// <param name="OrganizationName">The primary organization display name.</param>
/// <param name="TimeZoneId">The system or IANA time zone identifier for the organization.</param>
/// <param name="PrimaryCampusName">The primary campus display name.</param>
/// <param name="PrimaryCampusShortCode">An optional short code for the primary campus.</param>
/// <param name="AdminUserName">The initial administrator login username.</param>
/// <param name="AdminEmail">The initial administrator email address.</param>
/// <param name="AdminDisplayName">The initial administrator display name.</param>
/// <param name="AdminPassword">The initial administrator plaintext password (hashed immediately).</param>
public sealed record CompleteOnboardingCommand(
    string OrganizationName,
    string TimeZoneId,
    string PrimaryCampusName,
    string? PrimaryCampusShortCode,
    string AdminUserName,
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword
) : ICommand<CompleteOnboardingResponse>;
