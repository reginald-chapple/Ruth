using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Development in-memory email transport capturing rendered emails with CRLF header validation.</summary>
public sealed class DevInMemoryEmailTransport(InMemoryCommunicationSink sink, IClock clock)
    : ICommunicationTransport
{
    public const string Key = "dev-in-memory-email";

    public string TransportKey => Key;
    public string DisplayName => "Development In-Memory Email Sink";
    public CommunicationChannel Channel => CommunicationChannel.Email;
    public bool IsConfigured => true;

    public Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            return Task.FromResult(
                Result.Failure<TransportDeliveryReceipt>(
                    CommunicationDomainErrors.TransportRecipientRequired
                )
            );
        }

        var sanitizedResult = ValidateAndSanitizeMessage(message);
        if (sanitizedResult.IsFailure)
        {
            return Task.FromResult(
                Result.Failure<TransportDeliveryReceipt>(sanitizedResult.Errors)
            );
        }

        var now = clock.UtcNow;
        var providerId = $"dev-email-{message.MessageId:N}";
        sink.Record(sanitizedResult.Value, Key, providerId, now);
        return Task.FromResult(
            Result.Success(
                new TransportDeliveryReceipt(
                    Key,
                    CommunicationChannel.Email,
                    providerId,
                    now,
                    IsSimulated: true
                )
            )
        );
    }

    private static Result<OutboundTransportMessage> ValidateAndSanitizeMessage(
        OutboundTransportMessage message
    )
    {
        return
            !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ToAddress, out var cleanTo)
            || (
                !string.IsNullOrWhiteSpace(message.FromAddress)
                && !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.FromAddress, out _)
            )
            || (
                !string.IsNullOrWhiteSpace(message.ReplyToAddress)
                && !EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ReplyToAddress, out _)
            )
            ? DomainError.Validation(
                "Communication.EmailHeaderInjection",
                "Email recipient, sender, or reply-to address contains prohibited CR/LF or header-injection characters."
            )
            : message with
            {
                ToAddress = cleanTo,
                ToDisplayName = OptionalHeader(message.ToDisplayName),
                FromAddress = string.IsNullOrWhiteSpace(message.FromAddress)
                    ? null
                    : message.FromAddress.Trim(),
                FromDisplayName = OptionalHeader(message.FromDisplayName),
                ReplyToAddress = string.IsNullOrWhiteSpace(message.ReplyToAddress)
                    ? null
                    : message.ReplyToAddress.Trim(),
                Subject = EmailHeaderSanitizer.SanitizeHeaderValue(message.Subject),
            };
    }

    private static string? OptionalHeader(string? value)
    {
        var clean = EmailHeaderSanitizer.SanitizeHeaderValue(value);
        return string.IsNullOrEmpty(clean) ? null : clean;
    }
}
