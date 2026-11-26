using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Clean-room Apache-2.0 LiveKit HMAC-SHA256 room JWT minter and SHA-256 webhook verifier.</summary>
public static class LiveKitJwtAuthenticator
{
    private static readonly string _jwtHeaderSegment = Base64Url.EncodeToString(
        JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
    );

    /// <summary>Mints a short-lived room participant access token signed with HMAC-SHA256.</summary>
    public static string MintParticipantToken(
        string apiKey,
        string apiSecret,
        VirtualRoomTokenSpec spec,
        DateTimeOffset nowUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiSecret);
        ArgumentNullException.ThrowIfNull(spec);

        var issuedUnix = nowUtc.ToUnixTimeSeconds();
        var expUnix = nowUtc.Add(spec.Ttl).ToUnixTimeSeconds();
        var isModerator = spec.Role == VirtualParticipantRole.HostModerator;
        var canPublish = spec.Role != VirtualParticipantRole.Observer;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["iss"] = apiKey,
            ["sub"] = spec.ParticipantIdentity,
            ["jti"] = Guid.CreateVersion7(nowUtc).ToString("N"),
            ["nbf"] = issuedUnix - 5,
            ["exp"] = expUnix,
            ["name"] = spec.DisplayName,
            ["metadata"] = spec.MetadataJson,
            ["video"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["roomJoin"] = true,
                ["room"] = spec.LiveKitRoomName,
                ["roomAdmin"] = isModerator,
                ["roomRecord"] = isModerator,
                ["canPublish"] = canPublish,
                ["canSubscribe"] = true,
                ["canPublishData"] = canPublish,
            },
        };

        return SignPayload(payload, apiSecret);
    }

    /// <summary>Mints a short-lived server-to-server LiveKit Twirp admin JWT.</summary>
    public static string MintServerAdminToken(
        string apiKey,
        string apiSecret,
        string? roomName,
        DateTimeOffset nowUtc
    )
    {
        var issuedUnix = nowUtc.ToUnixTimeSeconds();
        var videoGrant = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["roomCreate"] = true,
            ["roomList"] = true,
            ["roomAdmin"] = true,
            ["roomRecord"] = true,
        };
        if (!string.IsNullOrWhiteSpace(roomName))
        {
            videoGrant["room"] = roomName;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["iss"] = apiKey,
            ["nbf"] = issuedUnix - 5,
            ["exp"] = issuedUnix + 300,
            ["video"] = videoGrant,
        };

        return SignPayload(payload, apiSecret);
    }

    /// <summary>Mints a signed LiveKit webhook Authorization JWT binding the SHA-256 hash of <paramref name="rawBodyUtf8" />.</summary>
    public static string SignWebhookPayload(
        string apiKey,
        string apiSecret,
        ReadOnlySpan<byte> rawBodyUtf8,
        DateTimeOffset nowUtc
    )
    {
        Span<byte> digest = stackalloc byte[32];
        _ = SHA256.HashData(rawBodyUtf8, digest);
        var sha256Base64 = Convert.ToBase64String(digest);
        var issuedUnix = nowUtc.ToUnixTimeSeconds();

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["iss"] = apiKey,
            ["nbf"] = issuedUnix - 5,
            ["exp"] = issuedUnix + 300,
            ["sha256"] = sha256Base64,
        };

        return SignPayload(payload, apiSecret);
    }

    /// <summary>Verifies a LiveKit webhook Authorization header JWT and constant-time SHA-256 payload digest.</summary>
    public static bool VerifyWebhook(
        string? authorizationHeader,
        ReadOnlySpan<byte> rawBodyUtf8,
        string apiKey,
        string apiSecret,
        DateTimeOffset nowUtc
    )
    {
        var token = LiveKitJwtEncodingHelper.ExtractBearerOrRawToken(authorizationHeader);
        return !string.IsNullOrWhiteSpace(token)
            && LiveKitJwtEncodingHelper.TrySplitJwt(
                token,
                out var header,
                out var payload,
                out var sig
            )
            && VerifySignatureConstantTime(header, payload, sig, apiSecret)
            && VerifyClaimsAndBodyHash(payload, rawBodyUtf8, apiKey, nowUtc);
    }

    private static string SignPayload(Dictionary<string, object?> payload, string apiSecret)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadSegment = Base64Url.EncodeToString(jsonBytes);
        var signingInput = $"{_jwtHeaderSegment}.{payloadSegment}";

        Span<byte> mac = stackalloc byte[32];
        _ = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(apiSecret),
            Encoding.ASCII.GetBytes(signingInput),
            mac
        );
        return $"{signingInput}.{Base64Url.EncodeToString(mac)}";
    }

    private static bool VerifySignatureConstantTime(
        string headerB64,
        string payloadB64,
        string signatureB64,
        string apiSecret
    )
    {
        byte[] providedSig;
        try
        {
            providedSig = Base64Url.DecodeFromChars(signatureB64);
        }
        catch (FormatException)
        {
            return false;
        }

        var signingInput = Encoding.ASCII.GetBytes($"{headerB64}.{payloadB64}");
        Span<byte> computed = stackalloc byte[32];
        _ = HMACSHA256.HashData(Encoding.UTF8.GetBytes(apiSecret), signingInput, computed);
        return CryptographicOperations.FixedTimeEquals(providedSig, computed);
    }

    private static bool VerifyClaimsAndBodyHash(
        string payloadB64,
        ReadOnlySpan<byte> rawBodyUtf8,
        string expectedApiKey,
        DateTimeOffset nowUtc
    )
    {
        try
        {
            var payloadBytes = Base64Url.DecodeFromChars(payloadB64);
            using var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;

            var iss = root.TryGetProperty("iss", out var issProp) ? issProp.GetString() : null;
            if (!string.Equals(iss, expectedApiKey, StringComparison.Ordinal))
            {
                return false;
            }

            if (
                root.TryGetProperty("exp", out var expProp)
                && expProp.TryGetInt64(out var expUnix)
                && expUnix + 60 < nowUtc.ToUnixTimeSeconds()
            )
            {
                return false;
            }

            var claimedSha = root.TryGetProperty("sha256", out var shaProp)
                ? shaProp.GetString()
                : null;
            return LiveKitJwtEncodingHelper.MatchesBodySha256(claimedSha, rawBodyUtf8);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }
    }
}
