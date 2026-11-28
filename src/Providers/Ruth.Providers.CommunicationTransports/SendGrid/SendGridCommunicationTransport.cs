using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.CommunicationTransports.SendGrid;

/// <summary>SendGrid v3 HTTP API email transport provider.</summary>
public sealed class SendGridCommunicationTransport(
    IHttpClientFactory httpClientFactory,
    IOptions<SendGridTransportOptions> options,
    IClock clock
) : ICommunicationTransport
{
    public const string Key = "sendgrid";
    public const string HttpClientName = "RuthSendGridTransport";
    private readonly SendGridTransportOptions _options = options.Value;

    public string TransportKey => Key;
    public string DisplayName => "SendGrid v3 Mail API";
    public CommunicationChannel Channel => CommunicationChannel.Email;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            return DomainError.Conflict(
                "SendGrid.NotConfigured",
                "SendGrid API key is not configured."
            );
        }

        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            return DomainError.Validation(
                "SendGrid.RecipientRequired",
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
                "SendGrid.HttpError",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"SendGrid API returned HTTP {(int)response.StatusCode}."
                )
            );
        }

        var messageId = response.Headers.TryGetValues("X-Message-Id", out var ids)
            ? ids.FirstOrDefault() ?? $"sg-{message.MessageId:N}"
            : $"sg-{message.MessageId:N}";

        return new TransportDeliveryReceipt(
            Key,
            CommunicationChannel.Email,
            messageId,
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
                "SendGrid.HeaderInjectionBlocked",
                "Email address contains prohibited CR/LF or header-injection characters."
            )
            : message with
            {
                ToAddress = cleanTo,
                FromAddress = cleanFrom,
                Subject = EmailHeaderSanitizer.SanitizeHeaderValue(message.Subject),
                ToDisplayName = EmailHeaderSanitizer.SanitizeHeaderValue(message.ToDisplayName),
                FromDisplayName = EmailHeaderSanitizer.SanitizeHeaderValue(
                    message.FromDisplayName ?? _options.DefaultFromName
                ),
            };
    }

    private HttpRequestMessage BuildHttpRequest(OutboundTransportMessage message)
    {
        var endpoint = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), "v3/mail/send");
        var fromEmail = string.IsNullOrWhiteSpace(message.FromAddress)
            ? _options.DefaultFromEmail
            : message.FromAddress;
        var fromName = message.FromDisplayName ?? _options.DefaultFromName;

        var contentParts = new List<object>(2);
        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            contentParts.Add(new { type = "text/plain", value = message.PlainTextBody });
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            contentParts.Add(new { type = "text/html", value = message.HtmlBody });
        }

        if (contentParts.Count == 0)
        {
            contentParts.Add(new { type = "text/plain", value = string.Empty });
        }

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = message.ToAddress, name = message.ToDisplayName } },
                },
            },
            from = new { email = fromEmail, name = fromName },
            subject = message.Subject ?? "(No Subject)",
            content = contentParts,
        };

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return req;
    }
}
