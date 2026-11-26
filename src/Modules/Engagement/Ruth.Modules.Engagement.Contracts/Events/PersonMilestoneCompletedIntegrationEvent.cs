using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when a constituent completes a JourneyProgram milestone.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when completed.</param>
/// <param name="PersonMilestoneId">Completed person milestone identifier.</param>
/// <param name="PersonId">Constituent person identifier.</param>
/// <param name="JourneyProgramId">Owning journey program identifier.</param>
/// <param name="MilestoneTypeId">Completed milestone type identifier.</param>
/// <param name="CompletedDate">Completion calendar date.</param>
public sealed record PersonMilestoneCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PersonMilestoneId,
    Guid PersonId,
    Guid JourneyProgramId,
    Guid MilestoneTypeId,
    DateOnly CompletedDate
) : IIntegrationEvent;
