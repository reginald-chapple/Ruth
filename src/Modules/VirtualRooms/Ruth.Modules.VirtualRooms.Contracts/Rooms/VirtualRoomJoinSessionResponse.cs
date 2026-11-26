using Ruth.Modules.VirtualRooms.Contracts.Providers;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Payload returned to the Blazor LiveKit stage island or API client when joining a VirtualRoom.</summary>
/// <param name="VirtualRoomId">VirtualRoom identifier.</param>
/// <param name="SessionId">Active VirtualRoomSession identifier.</param>
/// <param name="LiveKitRoomName">Canonical LiveKit room name.</param>
/// <param name="RoomDisplayName">Human-readable room name.</param>
/// <param name="ParticipantIdentity">Assigned participant identity.</param>
/// <param name="GrantedRole">ACL-mapped participant media role.</param>
/// <param name="WebSocketUrl">LiveKit WebSocket signaling URL.</param>
/// <param name="AccessToken">Short-lived signed HMAC-SHA256 JWT.</param>
/// <param name="ExpiresAtUtc">Token expiration UTC timestamp.</param>
public sealed record VirtualRoomJoinSessionResponse(
    Guid VirtualRoomId,
    Guid SessionId,
    string LiveKitRoomName,
    string RoomDisplayName,
    string ParticipantIdentity,
    VirtualParticipantRole GrantedRole,
    string WebSocketUrl,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc
);
