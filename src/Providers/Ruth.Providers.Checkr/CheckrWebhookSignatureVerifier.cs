using System.Security.Cryptography;
using System.Text;

namespace Ruth.Providers.Checkr;

/// <summary>Constant-time HMAC-SHA256 signature generator and verifier for Checkr X-Checkr-Signature headers.</summary>
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

    /// <summary>Verifies <paramref name="signatureHeader" /> against the expected HMAC-SHA256 digest in constant time.</summary>
    public static bool Verify(string rawPayload, string? signatureHeader, string secret)
    {
        if (
            string.IsNullOrWhiteSpace(rawPayload)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(secret)
        )
        {
            return false;
        }

        var expected = ComputeSignature(rawPayload, secret);
        var provided = signatureHeader.Trim();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToLowerInvariant());

        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
