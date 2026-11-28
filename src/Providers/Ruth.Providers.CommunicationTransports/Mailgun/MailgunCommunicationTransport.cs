using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.CommunicationTransports.Mailgun;

/// <summary>Mailgun v3 Messages API email transport provider.</summary>
public sealed class MailgunCommunicationTransport(
    IHttpClientFactory httpClientFactory,
    IOptions<MailgunTransportOptions> options,
    IClock clock
) : ICommunicationTransport
{
    public const string Key = "mailgun";
    public const string HttpClientName = "RuthMailgunTransport";
    private readonly MailgunTransportOptions _options = options.Value;

    public string TransportKey => Key;
    public string DisplayName => "Mailgun Messages API";
    public CommunicationChannel Channel => CommunicationChannel.Email;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.Domain);

    public async Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            return DomainError.Conflict(
                "Mailgun.NotConfigured",
                "Mailgun API key and domain are required."
            );
        }

        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            return DomainError.Validation(
                "Mailgun.RecipientRequired",
                "Recipient email address is required."
            );
        }

        var sanitized = SanitizeOutboundMessage(message);
        if (sanitized.IsFailure)
        {
            return Result.Failure<TransportDeliveryReceipt>(sanitized.Errors);
        }

        using var request = BuildHttpRequest(sanitized.Value);
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return DomainError.Failure(
                "Mailgun.HttpError",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Mailgun API returned HTTP {(int)response.StatusCode}."
                )
            );
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var providerId = TryExtractJsonProperty(body, "id") ?? $"mg-{message.MessageId:N}";
        return new TransportDeliveryReceipt(
            Key,
            CommunicationChannel.Email,
            providerId,
            clock.UtcNow,
            IsSimulated: false
        );
    }

    private Result<OutboundTransportMessage> SanitizeOutboundMessage(
        OutboundTransportMessage message
    )
    {
        var rawFrom = string.IsNullOrWhiteSpace(message.FromAddress)
            ? _options.DefaultFromEmail
            : message.FromAddress;
        return
            !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ToAddress, out var cleanTo)
            || !EmailHeaderSanitizer.TrySanitizeEmailAddress(rawFrom, out var cleanFrom)
            || (
                !string.IsNullOrWhiteSpace(message.ReplyToAddress)
                && !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ReplyToAddress, out _)
            )
            ? DomainError.Validation(
                "Mailgun.HeaderInjectionBlocked",
                "Email address contains prohibited CR/LF or header-injection characters."
            )
            : message with
            {
                ToAddress = cleanTo,
                FromAddress = cleanFrom,
                Subject = EmailHeaderSanitizer.SanitizeHeaderValue(message.Subject),
                FromDisplayName = EmailHeaderSanitizer.SanitizeHeaderValue(
                    message.FromDisplayName ?? _options.DefaultFromName
                ),
            };
    }

    private HttpRequestMessage BuildHttpRequest(OutboundTransportMessage message)
    {
        var endpoint = new Uri(
            new Uri(_options.BaseUrl.TrimEnd('/') + "/"),
            $"v3/{Uri.EscapeDataString(_options.Domain!.Trim())}/messages"
        );
        var fromEmail = string.IsNullOrWhiteSpace(message.FromAddress)
            ? _options.DefaultFromEmail
            : message.FromAddress;
        var fromName = message.FromDisplayName ?? _options.DefaultFromName;

        var fields = new List<KeyValuePair<string, string>>
        {
            new("from", $"{fromName} <{fromEmail}>"),
            new("to", message.ToAddress),
            new("subject", message.Subject ?? "(No Subject)"),
        };
        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            fields.Add(new("text", message.PlainTextBody));
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            fields.Add(new("html", message.HtmlBody));
        }

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{_options.ApiKey}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        return req;
    }

    private static string? TryExtractJsonProperty(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(propertyName, out var prop)
                ? prop.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
