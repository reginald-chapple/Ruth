using Ruth.SharedKernel.Security;

namespace Ruth.Providers.PaymentGateways.Stripe;

internal static class StripeWebhookSignatureVerifier
{
    public static bool Verify(
        string rawBody,
        string? stripeSignatureHeader,
        string? webhookSecret,
        DateTimeOffset utcNow,
        TimeSpan? tolerance = null
    )
    {
        return WebhookHmacReplayVerifier.VerifyTimestampedHeader(
            rawBody,
            stripeSignatureHeader,
            webhookSecret,
            utcNow,
            signatureKey: "v1",
            tolerance: tolerance
        );
    }
}
