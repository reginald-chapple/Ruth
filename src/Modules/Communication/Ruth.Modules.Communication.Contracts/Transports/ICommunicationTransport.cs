using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Modular provider abstraction for dispatching email, SMS, or push notifications.</summary>
public interface ICommunicationTransport
{
    /// <summary>Gets the unique lowercase key identifying this transport provider.</summary>
    string TransportKey { get; }

    /// <summary>Gets the human-readable provider display name.</summary>
    string DisplayName { get; }

    /// <summary>Gets the communication channel served by this transport.</summary>
    CommunicationChannel Channel { get; }

    /// <summary>Gets a value indicating whether required credentials/endpoints are configured for live dispatch.</summary>
    bool IsConfigured { get; }

    /// <summary>Dispatches <paramref name="message" /> through this transport provider.</summary>
    /// <param name="message">The outbound transport message payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<TransportDeliveryReceipt>> SendAsync(
        OutboundTransportMessage message,
        CancellationToken cancellationToken = default
    );
}
