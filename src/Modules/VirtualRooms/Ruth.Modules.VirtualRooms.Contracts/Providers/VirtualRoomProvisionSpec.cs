namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Specification passed to IVirtualRoomProvider when ensuring a LiveKit WebRTC room exists.</summary>
/// <param name="LiveKitRoomName">Canonical LiveKit room name.</param>
/// <param name="MaxParticipants">Maximum simultaneous participants.</param>
/// <param name="EmptyTimeoutSeconds">Idle room auto-close timeout in seconds.</param>
/// <param name="MetadataJson">Optional room metadata JSON.</param>
public sealed record VirtualRoomProvisionSpec(
    string LiveKitRoomName,
    int MaxParticipants,
    int EmptyTimeoutSeconds,
    string? MetadataJson = null
);
