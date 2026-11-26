using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Outcome of rendering and dispatching a transactional system communication.</summary>
/// <param name="SystemCommunicationId">Resolved system communication identifier.</param>
/// <param name="SystemCommunicationKey">Resolved system communication key.</param>
/// <param name="DispatchedCount">Total channel/device messages dispatched.</param>
/// <param name="Receipts">Delivery receipts per dispatched transport message.</param>
/// <param name="RenderedEmailHtml">Rendered HTML email document when email channel ran.</param>
/// <param name="RenderedSmsBody">Rendered SMS body when SMS channel ran.</param>
/// <param name="RenderedPushBody">Rendered push notification body when Push channel ran.</param>
public sealed record SystemCommunicationSendResultResponse(
    Guid SystemCommunicationId,
    string SystemCommunicationKey,
    int DispatchedCount,
    IReadOnlyList<TransportDeliveryReceipt> Receipts,
    string? RenderedEmailHtml = null,
    string? RenderedSmsBody = null,
    string? RenderedPushBody = null
);
