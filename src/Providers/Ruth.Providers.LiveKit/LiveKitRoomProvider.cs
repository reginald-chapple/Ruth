using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.LiveKit;

/// <summary>Apache-2.0 LiveKit Twirp HTTP room &amp; RustFS S3 egress provider.</summary>
public sealed class LiveKitRoomProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LiveKitOptions> options,
    IClock clock
) : IVirtualRoomProvider
{
    /// <summary>Named HttpClient key.</summary>
    public const string HttpClientName = "LiveKitTwirpClient";

    /// <summary>Provider key.</summary>
    public const string Key = "livekit";

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public async Task<Result<VirtualRoomProvisionResult>> EnsureRoomAsync(
        VirtualRoomProvisionSpec spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var cfg = options.Value;
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = spec.LiveKitRoomName,
            ["max_participants"] = spec.MaxParticipants,
            ["empty_timeout"] = spec.EmptyTimeoutSeconds,
            ["metadata"] = spec.MetadataJson ?? string.Empty,
        };

        using var response = await SendTwirpAsync(
            cfg,
            "/twirp/livekit.RoomService/CreateRoom",
            spec.LiveKitRoomName,
            payload,
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<VirtualRoomProvisionResult>(
                BuildHttpError("LiveKit.CreateRoomFailed", "CreateRoom", response.StatusCode)
            );
        }

        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        var sid = doc.RootElement.TryGetProperty("sid", out var sidProp)
            ? sidProp.GetString() ?? $"RM_{Guid.NewGuid():N}"
            : $"RM_{Guid.NewGuid():N}";

        return Result.Success(
            new VirtualRoomProvisionResult(sid, spec.LiveKitRoomName, cfg.PublicWebSocketUrl)
        );
    }

    /// <inheritdoc />
    public async Task<Result> CloseRoomAsync(
        string liveKitRoomName,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = options.Value;
        using var response = await SendTwirpAsync(
            cfg,
            "/twirp/livekit.RoomService/DeleteRoom",
            liveKitRoomName,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["room"] = liveKitRoomName },
            cancellationToken
        );
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(
                BuildHttpError("LiveKit.DeleteRoomFailed", "DeleteRoom", response.StatusCode)
            );
    }

    /// <inheritdoc />
    public Result<MintedVirtualRoomToken> MintAccessToken(
        VirtualRoomTokenSpec spec,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var cfg = options.Value;
        var jwt = LiveKitJwtAuthenticator.MintParticipantToken(
            cfg.ApiKey,
            cfg.ApiSecret,
            spec,
            nowUtc
        );
        return Result.Success(
            new MintedVirtualRoomToken(jwt, cfg.PublicWebSocketUrl, nowUtc.Add(spec.Ttl), spec.Role)
        );
    }

    /// <inheritdoc />
    public async Task<Result<VirtualRoomEgressResult>> StartRoomCompositeEgressAsync(
        VirtualRoomEgressSpec spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var cfg = options.Value;
        var bucket = string.IsNullOrWhiteSpace(spec.StorageBucket)
            ? cfg.S3Bucket
            : spec.StorageBucket;
        var payload = BuildCompositeEgressPayload(cfg, spec, bucket);

        using var response = await SendTwirpAsync(
            cfg,
            "/twirp/livekit.Egress/StartRoomCompositeEgress",
            spec.LiveKitRoomName,
            payload,
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<VirtualRoomEgressResult>(
                BuildHttpError(
                    "LiveKit.StartEgressFailed",
                    "StartRoomCompositeEgress",
                    response.StatusCode
                )
            );
        }

        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        var egressId = doc.RootElement.TryGetProperty("egress_id", out var egProp)
            ? egProp.GetString() ?? $"EG_{Guid.NewGuid():N}"
            : $"EG_{Guid.NewGuid():N}";

        return Result.Success(new VirtualRoomEgressResult(egressId, bucket, spec.StorageObjectKey));
    }

    /// <inheritdoc />
    public async Task<Result> StopEgressAsync(
        string egressId,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = options.Value;
        using var response = await SendTwirpAsync(
            cfg,
            "/twirp/livekit.Egress/StopEgress",
            roomName: null,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["egress_id"] = egressId },
            cancellationToken
        );
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(
                BuildHttpError("LiveKit.StopEgressFailed", "StopEgress", response.StatusCode)
            );
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(
        string? authorizationHeader,
        ReadOnlySpan<byte> rawBodyUtf8,
        DateTimeOffset nowUtc
    )
    {
        var cfg = options.Value;
        return LiveKitJwtAuthenticator.VerifyWebhook(
            authorizationHeader,
            rawBodyUtf8,
            cfg.ApiKey,
            cfg.ApiSecret,
            nowUtc
        );
    }

    private static DomainError BuildHttpError(
        string code,
        string operation,
        HttpStatusCode statusCode
    )
    {
        return DomainError.Failure(
            code,
            string.Create(
                CultureInfo.InvariantCulture,
                $"LiveKit {operation} returned HTTP {(int)statusCode}."
            )
        );
    }

    private async Task<HttpResponseMessage> SendTwirpAsync(
        LiveKitOptions cfg,
        string relativePath,
        string? roomName,
        Dictionary<string, object?> payload,
        CancellationToken ct
    )
    {
        var baseUri = new Uri(cfg.ServerUrl.TrimEnd('/'), UriKind.Absolute);
        var endpoint = new Uri(baseUri, relativePath);
        var adminJwt = LiveKitJwtAuthenticator.MintServerAdminToken(
            cfg.ApiKey,
            cfg.ApiSecret,
            roomName,
            clock.UtcNow
        );

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminJwt);
        return await client.SendAsync(req, ct);
    }

    private static Dictionary<string, object?> BuildCompositeEgressPayload(
        LiveKitOptions cfg,
        VirtualRoomEgressSpec spec,
        string bucket
    )
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["room_name"] = spec.LiveKitRoomName,
            ["layout"] = spec.Layout,
            ["file_outputs"] = new object[]
            {
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["file_type"] = "MP4",
                    ["filepath"] = spec.StorageObjectKey,
                    ["s3"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["access_key"] = cfg.S3AccessKey,
                        ["secret"] = cfg.S3SecretKey,
                        ["bucket"] = bucket,
                        ["endpoint"] = cfg.S3Endpoint,
                        ["force_path_style"] = true,
                    },
                },
            },
        };
    }
}
