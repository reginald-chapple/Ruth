using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.CommunicationTransports.Smtp;

/// <summary>RFC 5321 SMTP email transport provider.</summary>
public sealed class SmtpCommunicationTransport(IOptions<SmtpTransportOptions> options, IClock clock)
    : ICommunicationTransport
{
    public const string Key = "smtp";
    private readonly SmtpTransportOptions _options = options.Value;

    public string TransportKey => Key;
    public string DisplayName => "SMTP Mail Relay";
    public CommunicationChannel Channel => CommunicationChannel.Email;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Host) && _options.Port is > 0 and <= 65535;

    public async Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            return DomainError.Conflict("Smtp.NotConfigured", "SMTP host/port is not configured.");
        }

        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            return DomainError.Validation(
                "Smtp.RecipientRequired",
                "Recipient email address is required."
            );
        }

        var addresses = ResolveAndSanitizeAddresses(message);
        if (addresses.IsFailure)
        {
            return Result.Failure<TransportDeliveryReceipt>(addresses.Errors);
        }

        var (cleanTo, fromEmail) = addresses.Value;
        var safeMessage = message with { ToAddress = cleanTo };
        var fromName = EmailHeaderSanitizer.SanitizeHeaderValue(
            message.FromDisplayName ?? _options.DefaultFromName
        );
        var mime = SmtpMimeBuilder.BuildMimePayload(safeMessage, fromEmail, fromName, clock.UtcNow);

        try
        {
            var delivered = await SmtpProtocolSession.DeliverOverTcpAsync(
                _options,
                fromEmail,
                cleanTo,
                mime,
                message.MessageId,
                cancellationToken
            );
            return delivered.IsFailure
                ? Result.Failure<TransportDeliveryReceipt>(delivered.Errors)
                : new TransportDeliveryReceipt(
                    Key,
                    CommunicationChannel.Email,
                    delivered.Value,
                    clock.UtcNow,
                    IsSimulated: false
                );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DomainError.Failure(
                "Smtp.DeliveryFailed",
                $"SMTP delivery failed: {ex.Message}"
            );
        }
    }

    private Result<(string CleanTo, string FromEmail)> ResolveAndSanitizeAddresses(
        OutboundTransportMessage message
    )
    {
        var rawFrom = string.IsNullOrWhiteSpace(message.FromAddress)
            ? _options.DefaultFromEmail
            : message.FromAddress;
        return
            !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ToAddress, out var cleanTo)
            || !EmailHeaderSanitizer.TrySanitizeEmailAddress(rawFrom, out var fromEmail)
            || (
                !string.IsNullOrWhiteSpace(message.ReplyToAddress)
                && !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ReplyToAddress, out _)
            )
            ? DomainError.Validation(
                "Smtp.HeaderInjectionBlocked",
                "Email address contains prohibited CR/LF or SMTP command injection characters."
            )
            : (cleanTo, fromEmail);
    }
}
