using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Onboarding;

/// <summary>HTTP request payload for completing initial system onboarding.</summary>
/// <param name="OrganizationName">The organization display name.</param>
/// <param name="TimeZoneId">The system or IANA time zone identifier.</param>
/// <param name="PrimaryCampusName">The primary campus display name.</param>
/// <param name="PrimaryCampusShortCode">An optional primary campus short code.</param>
/// <param name="AdminUserName">The initial administrator login username.</param>
/// <param name="AdminEmail">The initial administrator email address.</param>
/// <param name="AdminDisplayName">The initial administrator display name.</param>
/// <param name="AdminPassword">The initial administrator plaintext password.</param>
public sealed record CompleteOnboardingRequest(
    [property: Required] string OrganizationName,
    [property: Required] string TimeZoneId,
    [property: Required] string PrimaryCampusName,
    string? PrimaryCampusShortCode,
    [property: Required] string AdminUserName,
    [property: Required, EmailAddress] string AdminEmail,
    [property: Required] string AdminDisplayName,
    [property: Required] string AdminPassword
);
