using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when a constituent completes an entire JourneyProgram.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when completed.</param>
/// <param name="JourneyCompletionId">Journey completion record identifier.</param>
/// <param name="PersonId">Constituent person identifier.</param>
/// <param name="JourneyProgramId">Completed journey program identifier.</param>
/// <param name="CompletedDate">Completion calendar date.</param>
public sealed record JourneyProgramCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid JourneyCompletionId,
    Guid PersonId,
    Guid JourneyProgramId,
    DateOnly CompletedDate
) : IIntegrationEvent;
