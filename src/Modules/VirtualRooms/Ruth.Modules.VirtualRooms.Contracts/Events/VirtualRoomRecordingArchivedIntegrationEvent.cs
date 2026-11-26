using Ruth.SharedKernel.Events;

namespace Ruth.Modules.VirtualRooms.Contracts.Events;

/// <summary>Outbox integration event emitted when a LiveKit RustFS S3 recording archive completes (ready for Wave 6 pgvector transcript search).</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when archived.</param>
/// <param name="VirtualRoomId">Owning VirtualRoom identifier.</param>
/// <param name="SessionId">Owning VirtualRoomSession identifier.</param>
/// <param name="RecordingArchiveId">Recording archive identifier.</param>
/// <param name="EgressId">LiveKit egress identifier.</param>
/// <param name="StorageBucket">RustFS S3 bucket name.</param>
/// <param name="StorageObjectKey">RustFS S3 MP4 object key.</param>
/// <param name="TranscriptObjectKey">Deterministic Wave 6 transcript object key.</param>
/// <param name="DurationSeconds">Recording duration in seconds.</param>
public sealed record VirtualRoomRecordingArchivedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid VirtualRoomId,
    Guid SessionId,
    Guid RecordingArchiveId,
    string EgressId,
    string StorageBucket,
    string StorageObjectKey,
    string? TranscriptObjectKey,
    int DurationSeconds
) : IIntegrationEvent;
