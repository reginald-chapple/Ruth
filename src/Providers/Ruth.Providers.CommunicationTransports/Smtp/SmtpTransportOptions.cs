namespace Ruth.Providers.CommunicationTransports.Smtp;

/// <summary>Configuration options for the RFC 5321 SMTP email transport provider.</summary>
public sealed class SmtpTransportOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Communication:Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseImplicitTls { get; set; }
    public bool UseStartTls { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DefaultFromEmail { get; set; } = "notifications@ruth.local";
    public string DefaultFromName { get; set; } = "Ruth Notifications";
    public int ConnectTimeoutMs { get; set; } = 5_000;
}
