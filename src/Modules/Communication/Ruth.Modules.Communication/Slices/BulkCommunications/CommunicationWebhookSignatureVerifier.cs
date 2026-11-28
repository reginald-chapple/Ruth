using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

/// <summary>Constant-time HMAC-SHA256 signature and replay-timestamp verifier for X-Ruth-Webhook-Signature headers.</summary>
public static class CommunicationWebhookSignatureVerifier
{
    public const string HeaderName = "X-Ruth-Webhook-Signature";
    public const string TimestampHeaderName = "X-Ruth-Webhook-Timestamp";

    /// <summary>Computes lowercase hex HMAC-SHA256 over <paramref name="rawPayload" />.</summary>
    public static string ComputeSignature(string rawPayload, string secret)
    {
        ArgumentNullException.ThrowIfNull(rawPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(rawPayload)
        );
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Creates a timestamped <c lang="text">t=&lt;unix&gt;,v1=&lt;hex&gt;</c> header value over <paramref name="rawPayload" />.</summary>
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

    /// <summary>
    /// Verifies a timestamped webhook signature in constant time and enforces <paramref name="tolerance" /> around <paramref name="utcNow" />.
    /// Fails closed whenever <paramref name="secret" /> or <paramref name="signatureHeader" /> is missing.
    /// </summary>
    public static bool VerifyTimestamped(
        string? rawPayload,
        string? signatureHeader,
        string? timestampHeader,
        string? secret,
        DateTimeOffset utcNow,
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

        var trimmedSig = signatureHeader.Trim();
        var effectiveTolerance = tolerance ?? WebhookHmacReplayVerifier.DefaultReplayTolerance;

        if (trimmedSig.StartsWith("t=", StringComparison.Ordinal))
        {
            return WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                rawPayload,
                trimmedSig,
                secret,
                utcNow,
                signatureKey: "v1",
                tolerance: effectiveTolerance
            );
        }

        if (
            string.IsNullOrWhiteSpace(timestampHeader)
            || !long.TryParse(
                timestampHeader.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var unixTs
            )
            || !WebhookHmacReplayVerifier.IsWithinReplayWindow(unixTs, utcNow, effectiveTolerance)
        )
        {
            return false;
        }

        var signedContent = string.Create(CultureInfo.InvariantCulture, $"{unixTs}.{rawPayload}");
        return WebhookHmacReplayVerifier.VerifyRawHmacHexConstantTime(
            signedContent,
            trimmedSig,
            secret
        );
    }
}
