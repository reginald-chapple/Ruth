namespace Ruth.Providers.CommunicationTransports.Twilio;

/// <summary>Configuration options for the Twilio Programmable SMS transport provider.</summary>
public sealed class TwilioSmsTransportOptions
{
    public const string SectionName = "Communication:TwilioSms";

    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? DefaultFromNumber { get; set; }
    public string BaseUrl { get; set; } = "https://api.twilio.com";
}
