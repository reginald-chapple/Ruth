namespace Ruth.Providers.PaymentGateways.Nmi;

/// <summary>Configuration options for the NMI Direct Post and Customer Vault payment gateway provider.</summary>
public sealed class NmiGatewayOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PaymentGateways:Nmi";

    /// <summary>Gets or sets the NMI Direct Post API endpoint URL.</summary>
    public string EndpointUrl { get; set; } = "https://secure.nmi.com/api/transact.php";

    /// <summary>Gets or sets the merchant private security key.</summary>
    public string? SecurityKey { get; set; }

    /// <summary>Gets or sets the webhook signing key used to verify NMI Webhook-Signature headers.</summary>
    public string? WebhookSigningKey { get; set; }
}
