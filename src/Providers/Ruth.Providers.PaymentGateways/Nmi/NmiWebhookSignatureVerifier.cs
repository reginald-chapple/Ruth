using Ruth.SharedKernel.Security;

namespace Ruth.Providers.PaymentGateways.Nmi;

internal static class NmiWebhookSignatureVerifier
{
    public static bool Verify(
        string rawBody,
        string? signatureHeader,
        string? signingKey,
        DateTimeOffset utcNow,
        TimeSpan? tolerance = null
    )
    {
        return WebhookHmacReplayVerifier.VerifyTimestampedHeader(
            rawBody,
            signatureHeader,
            signingKey,
            utcNow,
            signatureKey: "s",
            tolerance: tolerance
        );
    }
}
