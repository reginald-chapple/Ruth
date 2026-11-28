using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ruth.SharedKernel.Security;

namespace Ruth.Providers.Checkr;

/// <summary>Constant-time HMAC-SHA256 signature generator and replay-window verifier for Checkr X-Checkr-Signature headers.</summary>
public static class CheckrWebhookSignatureVerifier
{
    /// <summary>Computes the lowercase hex HMAC-SHA256 digest of <paramref name="rawPayload" />.</summary>
    public static string ComputeSignature(string rawPayload, string secret)
    {
        ArgumentNullException.ThrowIfNull(rawPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Creates a timestamped <c lang="text">t=&lt;unix&gt;,v1=&lt;hex&gt;</c> Checkr webhook signature header.</summary>
    public static string CreateTimestampedSignature(
        string rawPayload,
        string secret,
        DateTimeOffset timestampUtc
    )
    {
        return WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            rawPayload,
            secret,
            timestampUtc,
            signatureKey: "v1"
        );
    }

    /// <summary>Verifies <paramref name="signatureHeader" /> against the expected HMAC-SHA256 digest in constant time and enforces optional replay-timestamp bounds.</summary>
    public static bool Verify(
        string rawPayload,
        string? signatureHeader,
        string secret,
        DateTimeOffset? utcNow = null,
        TimeSpan? tolerance = null
    )
    {
        if (
            string.IsNullOrWhiteSpace(rawPayload)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(secret)
        )
        {
            return false;
        }

        var trimmed = signatureHeader.Trim();
        var effectiveNow = utcNow ?? TimeProvider.System.GetUtcNow();
        var effectiveTolerance = tolerance ?? WebhookHmacReplayVerifier.DefaultReplayTolerance;

        return trimmed.StartsWith("t=", StringComparison.Ordinal)
            ? WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                rawPayload,
                trimmed,
                secret,
                effectiveNow,
                signatureKey: "v1",
                tolerance: effectiveTolerance
            )
            : WebhookHmacReplayVerifier.VerifyRawHmacHexConstantTime(rawPayload, trimmed, secret)
                && IsPayloadCreatedAtWithinWindow(rawPayload, effectiveNow, effectiveTolerance);
    }

    private static bool IsPayloadCreatedAtWithinWindow(
        string rawPayload,
        DateTimeOffset utcNow,
        TimeSpan tolerance
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            return !doc.RootElement.TryGetProperty("created_at", out var createdProp)
                || IsCreatedElementWithinWindow(createdProp, utcNow, tolerance);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static bool IsCreatedElementWithinWindow(
        JsonElement createdProp,
        DateTimeOffset utcNow,
        TimeSpan tolerance
    )
    {
        return createdProp.ValueKind == JsonValueKind.Number && createdProp.TryGetInt64(out var ts)
            ? WebhookHmacReplayVerifier.IsWithinReplayWindow(ts, utcNow, tolerance)
            : createdProp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(
                    createdProp.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedDto
                )
                && (utcNow.ToUniversalTime() - parsedDto).Duration() <= tolerance;
    }
}
