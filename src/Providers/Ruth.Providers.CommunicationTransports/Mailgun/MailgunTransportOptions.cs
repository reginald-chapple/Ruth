namespace Ruth.Providers.CommunicationTransports.Mailgun;

/// <summary>Configuration options for the Mailgun v3 HTTP API email transport provider.</summary>
public sealed class MailgunTransportOptions
{
    public const string SectionName = "Communication:Mailgun";

    public string? ApiKey { get; set; }
    public string? Domain { get; set; }
    public string BaseUrl { get; set; } = "https://api.mailgun.net";
    public string DefaultFromEmail { get; set; } = "notifications@ruth.local";
    public string DefaultFromName { get; set; } = "Ruth Notifications";
}
