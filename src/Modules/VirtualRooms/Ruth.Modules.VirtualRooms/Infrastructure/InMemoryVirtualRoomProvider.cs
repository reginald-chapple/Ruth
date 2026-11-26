using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Infrastructure;

/// <summary>Deterministic in-memory IVirtualRoomProvider using real HMAC-SHA256 JWTs for local dev and automated tests.</summary>
public sealed class InMemoryVirtualRoomProvider(IConfiguration configuration) : IVirtualRoomProvider
{
    public const string Key = "in-memory";
    public const string DefaultApiKey = "devkey";
    public const string DefaultApiSecret = "ruth-inmemory-livekit-dev-signing-secret-32chars!";

    private readonly ConcurrentDictionary<string, string> _activeRooms = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ConcurrentDictionary<string, string> _activeEgresses = new(
        StringComparer.OrdinalIgnoreCase
    );

    public string ProviderKey => Key;

    public string ApiKey =>
        configuration["VirtualRooms:LiveKit:ApiKey"] is { Length: > 0 } configuredKey
            ? configuredKey
            : DefaultApiKey;

    public string ApiSecret =>
        configuration["VirtualRooms:LiveKit:ApiSecret"] is { Length: > 0 } configuredSecret
            ? configuredSecret
            : DefaultApiSecret;

    public string WebSocketUrl =>
        configuration["VirtualRooms:LiveKit:PublicWebSocketUrl"] is { Length: > 0 } ws
            ? ws
            : "ws://localhost:7880";

    public Task<Result<VirtualRoomProvisionResult>> EnsureRoomAsync(
        VirtualRoomProvisionSpec spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var sid = _activeRooms.GetOrAdd(spec.LiveKitRoomName, static _ =>
            $"RM_{Guid.NewGuid().ToString("N")[..12]}"
        );
        return Task.FromResult(
            Result.Success(new VirtualRoomProvisionResult(sid, spec.LiveKitRoomName, WebSocketUrl))
        );
    }

    public Task<Result> CloseRoomAsync(
        string liveKitRoomName,
        CancellationToken cancellationToken = default
    )
    {
        _ = _activeRooms.TryRemove(liveKitRoomName, out _);
        return Task.FromResult(Result.Success());
    }

    public Result<MintedVirtualRoomToken> MintAccessToken(
        VirtualRoomTokenSpec spec,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var jwt = LiveKitJwtAuthenticator.MintParticipantToken(ApiKey, ApiSecret, spec, nowUtc);
        return Result.Success(
            new MintedVirtualRoomToken(jwt, WebSocketUrl, nowUtc.Add(spec.Ttl), spec.Role)
        );
    }

    public Task<Result<VirtualRoomEgressResult>> StartRoomCompositeEgressAsync(
        VirtualRoomEgressSpec spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var egressId = $"EG_{Guid.NewGuid().ToString("N")[..12]}";
        _activeEgresses[egressId] = spec.StorageObjectKey;
        var bucket = string.IsNullOrWhiteSpace(spec.StorageBucket) ? "ruth" : spec.StorageBucket;
        return Task.FromResult(
            Result.Success(new VirtualRoomEgressResult(egressId, bucket, spec.StorageObjectKey))
        );
    }

    public Task<Result> StopEgressAsync(
        string egressId,
        CancellationToken cancellationToken = default
    )
    {
        _ = _activeEgresses.TryRemove(egressId, out _);
        return Task.FromResult(Result.Success());
    }

    public bool VerifyWebhookSignature(
        string? authorizationHeader,
        ReadOnlySpan<byte> rawBodyUtf8,
        DateTimeOffset nowUtc
    )
    {
        return LiveKitJwtAuthenticator.VerifyWebhook(
            authorizationHeader,
            rawBodyUtf8,
            ApiKey,
            ApiSecret,
            nowUtc
        );
    }
}
