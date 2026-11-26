using System.Security.Cryptography;
using System.Text;

namespace Ruth.Providers.PaymentGateways.Stripe;

internal static class StripeWebhookSignatureVerifier
{
    public static bool Verify(string rawBody, string? stripeSignatureHeader, string? webhookSecret)
    {
        if (
            string.IsNullOrWhiteSpace(rawBody)
            || string.IsNullOrWhiteSpace(stripeSignatureHeader)
            || string.IsNullOrWhiteSpace(webhookSecret)
        )
        {
            return false;
        }

        string? timestamp = null;
        string? v1Sig = null;
        foreach (var part in stripeSignatureHeader.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("t=", StringComparison.Ordinal))
            {
                timestamp = part[2..];
            }
            else if (part.StartsWith("v1=", StringComparison.Ordinal))
            {
                v1Sig = part[3..];
            }
        }

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(v1Sig))
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{rawBody}";
        var computedHex = Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(webhookSecret),
                Encoding.UTF8.GetBytes(signedPayload)
            )
        );

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(v1Sig.ToLowerInvariant())
        );
    }
}
