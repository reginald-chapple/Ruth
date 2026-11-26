namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Mobile and Web My Portfolio dashboard and prioritized outreach queue.</summary>
/// <param name="OwnerPersonId">Portfolio owner Person identifier (Guid.Empty when viewing all portfolios).</param>
/// <param name="ActiveContactCount">Total active contacts in portfolio.</param>
/// <param name="HealthyCount">Count of Healthy contacts.</param>
/// <param name="DueOrOverdueCount">Count of DueSoon or Overdue contacts.</param>
/// <param name="CriticalLapseCount">Count of CriticalLapse contacts.</param>
/// <param name="AverageHealthScore">Mean relationship health score (0..100).</param>
/// <param name="OutreachQueue">Prioritized pending/snoozed touchpoints ordered by urgency.</param>
/// <param name="RecentCompletedTouchpoints">Recently completed touchpoints.</param>
/// <param name="Contacts">Portfolio constituent contacts.</param>
public sealed record PortfolioOutreachQueueResponse(
    Guid OwnerPersonId,
    int ActiveContactCount,
    int HealthyCount,
    int DueOrOverdueCount,
    int CriticalLapseCount,
    int AverageHealthScore,
    IReadOnlyList<TouchpointResponse> OutreachQueue,
    IReadOnlyList<TouchpointResponse> RecentCompletedTouchpoints,
    IReadOnlyList<ConstituentContactResponse> Contacts
);
