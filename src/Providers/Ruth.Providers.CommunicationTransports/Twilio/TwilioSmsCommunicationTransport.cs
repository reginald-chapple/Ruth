using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.CommunicationTransports.Twilio;

/// <summary>Twilio Programmable Messaging REST API SMS transport provider.</summary>
public sealed class TwilioSmsCommunicationTransport(
    IHttpClientFactory httpClientFactory,
    IOptions<TwilioSmsTransportOptions> options,
    IClock clock
) : ICommunicationTransport
{
    public const string Key = "twilio-sms";
    public const string HttpClientName = "RuthTwilioSmsTransport";
    private readonly TwilioSmsTransportOptions _options = options.Value;

    public string TransportKey => Key;
    public string DisplayName => "Twilio Programmable SMS";
    public CommunicationChannel Channel => CommunicationChannel.Sms;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.AccountSid)
        && !string.IsNullOrWhiteSpace(_options.AuthToken);

    public async Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            return DomainError.Conflict(
                "TwilioSms.NotConfigured",
                "Twilio AccountSid and AuthToken are required."
            );
        }

        var fromNumber = string.IsNullOrWhiteSpace(message.FromAddress)
            ? _options.DefaultFromNumber
            : message.FromAddress;
        if (string.IsNullOrWhiteSpace(message.ToAddress) || string.IsNullOrWhiteSpace(fromNumber))
        {
            return DomainError.Validation(
                "TwilioSms.NumbersRequired",
                "Both To and From E.164 phone numbers are required."
            );
        }

        using var request = BuildHttpRequest(message, fromNumber);
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return DomainError.Failure(
                "TwilioSms.HttpError",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Twilio API returned HTTP {(int)response.StatusCode}."
                )
            );
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var sid = TryExtractSid(body) ?? $"SM{message.MessageId:N}";
        return new TransportDeliveryReceipt(
            Key,
            CommunicationChannel.Sms,
            sid,
            clock.UtcNow,
            IsSimulated: false
        );
    }

    private HttpRequestMessage BuildHttpRequest(OutboundTransportMessage message, string fromNumber)
    {
        var sid = Uri.EscapeDataString(_options.AccountSid!.Trim());
        var endpoint = new Uri(
            new Uri(_options.BaseUrl.TrimEnd('/') + "/"),
            $"2010-04-01/Accounts/{sid}/Messages.json"
        );
        var fields = new KeyValuePair<string, string>[]
        {
            new("To", message.ToAddress),
            new("From", fromNumber),
            new("Body", message.PlainTextBody ?? string.Empty),
        };

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}")
        );
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        return req;
    }

    private static string? TryExtractSid(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("sid", out var prop) ? prop.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
