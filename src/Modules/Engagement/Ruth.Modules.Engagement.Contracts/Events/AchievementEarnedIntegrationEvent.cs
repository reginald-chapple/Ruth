using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when a constituent earns an AchievementAttempt badge or certification.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when earned.</param>
/// <param name="AchievementAttemptId">Earned achievement attempt identifier.</param>
/// <param name="AchievementTypeId">Achievement type identifier.</param>
/// <param name="AchievementCode">Unique achievement type code.</param>
/// <param name="PersonId">Constituent person identifier.</param>
/// <param name="AchievedDate">Calendar date achieved.</param>
public sealed record AchievementEarnedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid AchievementAttemptId,
    Guid AchievementTypeId,
    string AchievementCode,
    Guid PersonId,
    DateOnly AchievedDate
) : IIntegrationEvent;
