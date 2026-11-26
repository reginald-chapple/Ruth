using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Push notification stub provider recording outbound mobile/web push payloads for verification.</summary>
public sealed class PushNotificationStubTransport(InMemoryCommunicationSink sink, IClock clock)
    : ICommunicationTransport
{
    public const string Key = "push-stub";

    public string TransportKey => Key;
    public string DisplayName => "Push Notification Stub Gateway";
    public CommunicationChannel Channel => CommunicationChannel.Push;
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
        var providerId = $"push-stub-{message.MessageId:N}";
        sink.Record(message, Key, providerId, now);
        return Task.FromResult(
            Result.Success(
                new TransportDeliveryReceipt(
                    Key,
                    CommunicationChannel.Push,
                    providerId,
                    now,
                    IsSimulated: true
                )
            )
        );
    }
}
