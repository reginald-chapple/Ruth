namespace Ruth.Host.Components.Pages;

/// <summary>Represents a selectable time zone entry in the onboarding wizard.</summary>
/// <param name="Id">The system or IANA time zone identifier.</param>
/// <param name="DisplayName">The human-readable display label.</param>
public sealed record TimeZoneOption(string Id, string DisplayName);
