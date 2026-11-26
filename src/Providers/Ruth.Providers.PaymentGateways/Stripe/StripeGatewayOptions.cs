namespace Ruth.Providers.PaymentGateways.Stripe;

/// <summary>Configuration options for the Stripe v1 REST payment gateway provider.</summary>
public sealed class StripeGatewayOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PaymentGateways:Stripe";

    /// <summary>Gets or sets the Stripe API base URL.</summary>
    public string BaseUrl { get; set; } = "https://api.stripe.com";

    /// <summary>Gets or sets the Stripe secret API key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Gets or sets the Stripe webhook endpoint signing secret (whsec_...).</summary>
    public string? WebhookSigningSecret { get; set; }
}
