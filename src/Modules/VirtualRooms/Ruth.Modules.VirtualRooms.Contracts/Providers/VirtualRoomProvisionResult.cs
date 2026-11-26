namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Provider response after ensuring a LiveKit WebRTC room exists.</summary>
/// <param name="LiveKitRoomSid">Server-assigned room SID.</param>
/// <param name="LiveKitRoomName">Canonical room name.</param>
/// <param name="WebSocketUrl">Client WebRTC signaling WebSocket URL.</param>
public sealed record VirtualRoomProvisionResult(
    string LiveKitRoomSid,
    string LiveKitRoomName,
    string WebSocketUrl
);
