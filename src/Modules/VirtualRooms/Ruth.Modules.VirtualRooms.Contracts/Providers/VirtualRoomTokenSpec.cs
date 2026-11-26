namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Parameters for minting a short-lived HMAC-SHA256 LiveKit room access JWT.</summary>
/// <param name="LiveKitRoomName">Target LiveKit room name.</param>
/// <param name="ParticipantIdentity">Unique participant identity string.</param>
/// <param name="DisplayName">Participant display name.</param>
/// <param name="Role">Mapped media permission role.</param>
/// <param name="Ttl">Short-lived token validity duration.</param>
/// <param name="MetadataJson">Optional participant metadata JSON.</param>
public sealed record VirtualRoomTokenSpec(
    string LiveKitRoomName,
    string ParticipantIdentity,
    string DisplayName,
    VirtualParticipantRole Role,
    TimeSpan Ttl,
    string? MetadataJson = null
);
