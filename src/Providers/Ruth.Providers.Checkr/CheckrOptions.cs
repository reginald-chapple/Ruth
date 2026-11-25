namespace Ruth.Providers.Checkr;

/// <summary>Configuration options for the modular Checkr background check provider.</summary>
public sealed class CheckrOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BackgroundChecks:Checkr";

    /// <summary>Gets or sets the Checkr REST API base URL.</summary>
    public Uri BaseUrl { get; set; } = new("https://api.checkr.com/");

    /// <summary>Gets or sets the Checkr API key used for HTTP Basic authentication.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the webhook signing secret (falls back to <see cref="ApiKey" /> when blank).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the default Checkr screening package slug.</summary>
    public string DefaultPackage { get; set; } = "tasker_standard";

    /// <summary>Returns the active signing key for HMAC-SHA256 webhook verification.</summary>
    public string EffectiveWebhookSecret =>
        !string.IsNullOrWhiteSpace(WebhookSecret) ? WebhookSecret : ApiKey;
}
