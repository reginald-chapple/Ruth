using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Development in-memory email transport capturing rendered emails without external network calls.</summary>
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

        var now = clock.UtcNow;
        var providerId = $"dev-email-{message.MessageId:N}";
        sink.Record(message, Key, providerId, now);
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
}
