namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Delivery acknowledgment receipt returned by an <see cref="ICommunicationTransport" />.</summary>
/// <param name="TransportKey">Registered transport provider key (e.g., smtp, sendgrid, mailgun, twilio-sms, push-stub, dev-in-memory-email).</param>
/// <param name="Channel">Channel over which the message was dispatched.</param>
/// <param name="ProviderMessageId">Provider-assigned correlation or message identifier.</param>
/// <param name="DeliveredOnUtc">UTC timestamp when the provider accepted the message.</param>
/// <param name="IsSimulated">Whether delivery was captured in a local/stub sink rather than an external network gateway.</param>
public sealed record TransportDeliveryReceipt(
    string TransportKey,
    CommunicationChannel Channel,
    string ProviderMessageId,
    DateTimeOffset DeliveredOnUtc,
    bool IsSimulated
);
