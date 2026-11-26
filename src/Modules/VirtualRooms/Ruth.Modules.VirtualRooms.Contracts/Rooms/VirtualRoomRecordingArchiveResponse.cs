namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Read projection of an archived RustFS S3 recording egress.</summary>
/// <param name="Id">Archive identifier.</param>
/// <param name="EgressId">LiveKit egress identifier.</param>
/// <param name="Status">Recording status.</param>
/// <param name="StorageBucket">RustFS S3 bucket name.</param>
/// <param name="StorageObjectKey">RustFS S3 object key.</param>
/// <param name="TranscriptObjectKey">Optional Wave 6 pgvector transcript object key.</param>
/// <param name="ContentType">Media MIME type.</param>
/// <param name="SizeBytes">Archived file byte size.</param>
/// <param name="DurationSeconds">Recording duration in seconds.</param>
/// <param name="StartedAtUtc">Start UTC timestamp.</param>
/// <param name="CompletedAtUtc">Optional completion UTC timestamp.</param>
/// <param name="ErrorMessage">Optional egress error message.</param>
public sealed record VirtualRoomRecordingArchiveResponse(
    Guid Id,
    string EgressId,
    VirtualRecordingStatus Status,
    string StorageBucket,
    string StorageObjectKey,
    string? TranscriptObjectKey,
    string ContentType,
    long SizeBytes,
    int DurationSeconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorMessage
);
