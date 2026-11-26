namespace Ruth.Providers.CommunicationTransports.SendGrid;

/// <summary>Configuration options for the SendGrid v3 Web API email transport provider.</summary>
public sealed class SendGridTransportOptions
{
    public const string SectionName = "Communication:SendGrid";

    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.sendgrid.com";
    public string DefaultFromEmail { get; set; } = "notifications@ruth.local";
    public string DefaultFromName { get; set; } = "Ruth Notifications";
}
