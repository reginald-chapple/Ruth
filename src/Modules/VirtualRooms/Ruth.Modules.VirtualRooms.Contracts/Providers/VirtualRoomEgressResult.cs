namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Provider response when starting or stopping a LiveKit recording egress.</summary>
/// <param name="EgressId">LiveKit egress job identifier.</param>
/// <param name="StorageBucket">Destination bucket.</param>
/// <param name="StorageObjectKey">Destination object key.</param>
public sealed record VirtualRoomEgressResult(
    string EgressId,
    string StorageBucket,
    string StorageObjectKey
);
