namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Minted short-lived LiveKit room JWT and connection details.</summary>
/// <param name="AccessToken">Signed HMAC-SHA256 LiveKit JWT.</param>
/// <param name="WebSocketUrl">LiveKit signaling WebSocket URL.</param>
/// <param name="ExpiresAtUtc">Token expiration UTC timestamp.</param>
/// <param name="Role">Granted participant role.</param>
public sealed record MintedVirtualRoomToken(
    string AccessToken,
    string WebSocketUrl,
    DateTimeOffset ExpiresAtUtc,
    VirtualParticipantRole Role
);
