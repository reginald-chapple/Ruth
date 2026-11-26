using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Lists archived RustFS S3 recordings across VirtualRoomSessions.</summary>
/// <param name="VirtualRoomId">Optional VirtualRoom filter.</param>
/// <param name="CompletedOnly">When true, returns only Completed recordings.</param>
public sealed record GetVirtualRoomRecordingsQuery(
    Guid? VirtualRoomId = null,
    bool CompletedOnly = false
) : IQuery<IReadOnlyList<VirtualRoomRecordingArchiveResponse>>;
