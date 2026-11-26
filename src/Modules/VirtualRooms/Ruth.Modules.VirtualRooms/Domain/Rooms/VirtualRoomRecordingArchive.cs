using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain.Rooms;

/// <summary>Tracks a RustFS S3 MP4 recording archive and Wave 6 transcript pointer for a VirtualRoomSession.</summary>
public sealed class VirtualRoomRecordingArchive : Entity<VirtualRoomRecordingArchiveId>
{
    private VirtualRoomRecordingArchive() { }

    private VirtualRoomRecordingArchive(VirtualRoomRecordingArchiveId id)
        : base(id) { }

    public VirtualRoomSessionId SessionId { get; private set; }
    public string EgressId { get; private set; } = string.Empty;
    public VirtualRecordingStatus Status { get; private set; } = VirtualRecordingStatus.Starting;
    public string StorageBucket { get; private set; } = string.Empty;
    public string StorageObjectKey { get; private set; } = string.Empty;
    public string? TranscriptObjectKey { get; private set; }
    public string ContentType { get; private set; } = "video/mp4";
    public long SizeBytes { get; private set; }
    public int DurationSeconds { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    internal static VirtualRoomRecordingArchive CreateStarting(
        VirtualRoomSessionId sessionId,
        string egressId,
        string storageBucket,
        string storageObjectKey,
        DateTimeOffset startedAtUtc
    )
    {
        var key = storageObjectKey.Trim();
        var transcriptKey = key.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            ? $"{key[..^4]}.transcript.json"
            : $"{key}.transcript.json";

        return new VirtualRoomRecordingArchive(VirtualRoomRecordingArchiveId.Create())
        {
            SessionId = sessionId,
            EgressId = egressId.Trim(),
            Status = VirtualRecordingStatus.Active,
            StorageBucket = storageBucket.Trim(),
            StorageObjectKey = key,
            TranscriptObjectKey = transcriptKey,
            ContentType = "video/mp4",
            StartedAtUtc = startedAtUtc.ToUniversalTime(),
        };
    }

    internal void MarkCompleted(
        long sizeBytes,
        int durationSeconds,
        string? overrideObjectKey,
        DateTimeOffset completedAtUtc
    )
    {
        Status = VirtualRecordingStatus.Completed;
        SizeBytes = Math.Max(0L, sizeBytes);
        DurationSeconds = Math.Max(0, durationSeconds);
        if (!string.IsNullOrWhiteSpace(overrideObjectKey))
        {
            StorageObjectKey = overrideObjectKey.Trim();
            TranscriptObjectKey = StorageObjectKey.EndsWith(
                ".mp4",
                StringComparison.OrdinalIgnoreCase
            )
                ? $"{StorageObjectKey[..^4]}.transcript.json"
                : $"{StorageObjectKey}.transcript.json";
        }

        CompletedAtUtc = completedAtUtc.ToUniversalTime();
        ErrorMessage = null;
    }

    internal void MarkFailed(string error, DateTimeOffset failedAtUtc)
    {
        Status = VirtualRecordingStatus.Failed;
        ErrorMessage = VirtualRoomsTextNormalizer.TrimOrNull(error, 1000) ?? "Egress failed.";
        CompletedAtUtc = failedAtUtc.ToUniversalTime();
    }
}
