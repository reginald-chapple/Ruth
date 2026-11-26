using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Modular strategy contract for Apache-2.0 LiveKit WebRTC room management, JWT minting, egress, and webhook verification.</summary>
public interface IVirtualRoomProvider
{
    /// <summary>Gets the canonical provider key (e.g., livekit or in-memory).</summary>
    string ProviderKey { get; }

    /// <summary>Ensures a room exists on the media server and returns its SID and signaling URL.</summary>
    Task<Result<VirtualRoomProvisionResult>> EnsureRoomAsync(
        VirtualRoomProvisionSpec spec,
        CancellationToken cancellationToken = default
    );

    /// <summary>Closes/deletes an active room on the media server.</summary>
    Task<Result> CloseRoomAsync(
        string liveKitRoomName,
        CancellationToken cancellationToken = default
    );

    /// <summary>Mints a short-lived HMAC-SHA256 LiveKit room JWT mapped from Ruth ACL permissions.</summary>
    Result<MintedVirtualRoomToken> MintAccessToken(
        VirtualRoomTokenSpec spec,
        DateTimeOffset nowUtc
    );

    /// <summary>Starts a room composite MP4 recording egress targeting RustFS S3 storage.</summary>
    Task<Result<VirtualRoomEgressResult>> StartRoomCompositeEgressAsync(
        VirtualRoomEgressSpec spec,
        CancellationToken cancellationToken = default
    );

    /// <summary>Requests graceful stop of an active recording egress.</summary>
    Task<Result> StopEgressAsync(string egressId, CancellationToken cancellationToken = default);

    /// <summary>Verifies the LiveKit Authorization JWT signature and constant-time SHA-256 body hash.</summary>
    bool VerifyWebhookSignature(
        string? authorizationHeader,
        ReadOnlySpan<byte> rawBodyUtf8,
        DateTimeOffset nowUtc
    );
}
