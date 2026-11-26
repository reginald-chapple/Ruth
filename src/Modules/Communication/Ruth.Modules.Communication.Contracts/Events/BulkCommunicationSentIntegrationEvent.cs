using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Communication.Contracts.Events;

/// <summary>Published via Outbox when a bulk or targeted Communication completes batch delivery.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when batch dispatch finished.</param>
/// <param name="CommunicationId">Completed communication identifier.</param>
/// <param name="Title">Campaign title.</param>
/// <param name="Channel">Channel used.</param>
/// <param name="TopicKey">Topic key.</param>
/// <param name="DeliveredCount">Recipients delivered.</param>
/// <param name="SkippedCount">Recipients skipped due to preferences.</param>
/// <param name="FailedCount">Recipients failed.</param>
public sealed record BulkCommunicationSentIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CommunicationId,
    string Title,
    CommunicationChannel Channel,
    string TopicKey,
    int DeliveredCount,
    int SkippedCount,
    int FailedCount
) : IIntegrationEvent;
