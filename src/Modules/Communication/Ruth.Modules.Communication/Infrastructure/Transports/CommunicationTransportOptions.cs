namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Configuration options governing default sender identity and preferred channel transports.</summary>
public sealed class CommunicationTransportOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Communication:Transports";

    /// <summary>Gets or sets the preferred email transport key (defaults to dev-in-memory-email when unconfigured).</summary>
    public string PreferredEmailTransport { get; set; } = "dev-in-memory-email";

    /// <summary>Gets or sets the preferred SMS transport key (defaults to dev-in-memory-sms when unconfigured).</summary>
    public string PreferredSmsTransport { get; set; } = "dev-in-memory-sms";

    /// <summary>Gets or sets the preferred push notification transport key (defaults to push-stub).</summary>
    public string PreferredPushTransport { get; set; } = "push-stub";

    /// <summary>Gets or sets the fallback sender email address.</summary>
    public string DefaultFromEmail { get; set; } = "notifications@ruth.local";

    /// <summary>Gets or sets the fallback sender display name.</summary>
    public string DefaultFromName { get; set; } = "Ruth Notifications";

    /// <summary>Gets or sets the default organization banner name for transactional emails.</summary>
    public string DefaultOrganizationName { get; set; } = "Ruth Community";
}
