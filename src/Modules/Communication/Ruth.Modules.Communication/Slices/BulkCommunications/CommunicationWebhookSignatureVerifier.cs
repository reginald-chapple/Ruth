using System.Security.Cryptography;
using System.Text;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

/// <summary>Constant-time HMAC-SHA256 signature helper for X-Ruth-Webhook-Signature headers.</summary>
public static class CommunicationWebhookSignatureVerifier
{
    public const string HeaderName = "X-Ruth-Webhook-Signature";

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

    /// <summary>Verifies <paramref name="signatureHeader" /> when <paramref name="secret" /> is configured.</summary>
    public static bool VerifyIfConfigured(
        string rawPayload,
        string? signatureHeader,
        string? secret
    )
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(ComputeSignature(rawPayload, secret));
        var provided = Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant());
        return expected.Length == provided.Length
            && CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
