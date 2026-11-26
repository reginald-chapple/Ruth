namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Specification for starting a LiveKit room composite S3/RustFS recording egress.</summary>
/// <param name="LiveKitRoomName">Target LiveKit room name.</param>
/// <param name="StorageBucket">Destination RustFS S3 bucket.</param>
/// <param name="StorageObjectKey">Destination object key path (e.g., virtual-rooms/recordings/...mp4).</param>
/// <param name="Layout">Composite layout preset (speaker, grid).</param>
public sealed record VirtualRoomEgressSpec(
    string LiveKitRoomName,
    string StorageBucket,
    string StorageObjectKey,
    string Layout = "grid"
);
