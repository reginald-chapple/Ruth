namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Represents a scheduled or completed outreach Touchpoint.</summary>
/// <param name="Id">Touchpoint identifier.</param>
/// <param name="ConstituentContactId">Owning ConstituentContact identifier.</param>
/// <param name="OwnerPersonId">Portfolio owner Person identifier.</param>
/// <param name="ConstituentPersonId">Constituent Person identifier.</param>
/// <param name="ConstituentDisplayName">Constituent display name.</param>
/// <param name="PortfolioRole">Portfolio role label.</param>
/// <param name="Kind">Touchpoint kind.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Channel">Outreach channel.</param>
/// <param name="ScheduledDate">Scheduled outreach date.</param>
/// <param name="CompletedAtUtc">Optional completion UTC timestamp.</param>
/// <param name="OutcomeSentiment">Optional recorded sentiment.</param>
/// <param name="Summary">Optional outreach summary.</param>
/// <param name="SnoozeCount">Count of times snoozed.</param>
public sealed record TouchpointResponse(
    Guid Id,
    Guid ConstituentContactId,
    Guid OwnerPersonId,
    Guid ConstituentPersonId,
    string ConstituentDisplayName,
    string PortfolioRole,
    TouchpointKind Kind,
    TouchpointStatus Status,
    TouchpointChannel Channel,
    DateOnly ScheduledDate,
    DateTimeOffset? CompletedAtUtc,
    TouchpointOutcomeSentiment? OutcomeSentiment,
    string? Summary,
    int SnoozeCount
);
