using System.Text.Json;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.VirtualRooms.Contracts.Events;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;

namespace Ruth.Modules.VirtualRooms.Slices.Webhooks;

internal sealed class LiveKitWebhookEgressCollaborator(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    IStorageProvider storageProvider
)
{
    public (bool AttendanceRecorded, bool RecordingArchived) HandleEgressStarted(
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed
    )
    {
        if (!string.IsNullOrWhiteSpace(parsed.EgressId))
        {
            _ = session.RegisterRecordingStart(
                parsed.EgressId,
                providerSelector.DefaultStorageBucket,
                parsed.EgressFilePath
                    ?? $"virtual-rooms/recordings/{session.Id.Value:D}/{parsed.EgressId}.mp4",
                parsed.OccurredAtUtc
            );
        }

        return (false, false);
    }

    public async Task<(bool AttendanceRecorded, bool RecordingArchived)> HandleEgressEndedAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(parsed.EgressId))
        {
            return (false, false);
        }

        var archive = session.CompleteOrFailRecording(
            parsed.EgressId,
            providerSelector.DefaultStorageBucket,
            parsed.EgressFilePath,
            parsed.EgressSizeBytes,
            parsed.EgressDurationSeconds,
            parsed.EgressError,
            parsed.OccurredAtUtc
        );

        if (archive.Status != VirtualRecordingStatus.Completed)
        {
            return (false, false);
        }

        await PersistTranscriptIndexStubAsync(room, session, archive, ct);
        dbContext.EnqueueIntegrationEvent(
            new VirtualRoomRecordingArchivedIntegrationEvent(
                Guid.CreateVersion7(parsed.OccurredAtUtc),
                parsed.OccurredAtUtc,
                room.Id.Value,
                session.Id.Value,
                archive.Id.Value,
                archive.EgressId,
                archive.StorageBucket,
                archive.StorageObjectKey,
                archive.TranscriptObjectKey,
                archive.DurationSeconds
            )
        );
        return (false, true);
    }

    private async Task PersistTranscriptIndexStubAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        VirtualRoomRecordingArchive archive,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(archive.TranscriptObjectKey))
        {
            return;
        }

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["virtualRoomId"] = room.Id.Value,
                ["roomName"] = room.Name,
                ["sessionId"] = session.Id.Value,
                ["sessionTitle"] = session.Title,
                ["egressId"] = archive.EgressId,
                ["recordingObjectKey"] = archive.StorageObjectKey,
                ["durationSeconds"] = archive.DurationSeconds,
                ["status"] = "pending_wave6_pgvector_indexing",
            }
        );

        await using var stream = new MemoryStream(manifestBytes, writable: false);
        _ = await storageProvider.UploadAsync(
            archive.TranscriptObjectKey,
            stream,
            "application/json",
            ct
        );
    }
}
