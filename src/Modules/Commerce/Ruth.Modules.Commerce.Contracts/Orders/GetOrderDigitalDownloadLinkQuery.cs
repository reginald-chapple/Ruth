using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Generates a presigned RustFS download link for a paid digital product line.</summary>
/// <param name="OrderNumberOrId">Order number or GUID.</param>
/// <param name="LineId">Digital order line identifier.</param>
/// <param name="ExpiresInMinutes">Presigned link lifetime in minutes.</param>
public sealed record GetOrderDigitalDownloadLinkQuery(
    string OrderNumberOrId,
    Guid LineId,
    int ExpiresInMinutes = 15
) : IQuery<OrderDigitalDownloadLinkResponse>;
