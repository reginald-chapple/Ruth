using System.Security.Cryptography;
using System.Text;

namespace Ruth.Providers.PaymentGateways.Nmi;

internal static class NmiWebhookSignatureVerifier
{
    public static bool Verify(string rawBody, string? signatureHeader, string? signingKey)
    {
        if (
            string.IsNullOrWhiteSpace(rawBody)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(signingKey)
        )
        {
            return false;
        }

        string? nonce = null;
        string? sigHex = null;
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("t=", StringComparison.Ordinal))
            {
                nonce = part[2..];
            }
            else if (part.StartsWith("s=", StringComparison.Ordinal))
            {
                sigHex = part[2..];
            }
        }

        if (string.IsNullOrWhiteSpace(nonce) || string.IsNullOrWhiteSpace(sigHex))
        {
            return false;
        }

        var signedPayload = $"{nonce}.{rawBody}";
        var computedHex = Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(signingKey),
                Encoding.UTF8.GetBytes(signedPayload)
            )
        );

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(sigHex.ToLowerInvariant())
        );
    }
}
