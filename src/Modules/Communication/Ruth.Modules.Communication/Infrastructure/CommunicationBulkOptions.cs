namespace Ruth.Modules.Communication.Infrastructure;

/// <summary>Configuration options for bulk communication preference tokens, public links, and webhook verification.</summary>
public sealed class CommunicationBulkOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Communication:BulkAndWebhooks";

    /// <summary>Gets or sets the HMAC-SHA256 signing secret used for unsubscribe and Preference Center tokens.</summary>
    public string PreferenceSigningSecret { get; set; } =
        "ruth-dev-preference-hmac-signing-key-change-in-production";

    /// <summary>Gets or sets the optional HMAC-SHA256 secret required on X-Ruth-Webhook-Signature headers when non-empty.</summary>
    public string? WebhookSigningSecret { get; set; }

    /// <summary>Gets or sets the public base URL prepended to unsubscribe and preference center links.</summary>
    public string PublicBaseUrl { get; set; } = "https://localhost:7175";
}
