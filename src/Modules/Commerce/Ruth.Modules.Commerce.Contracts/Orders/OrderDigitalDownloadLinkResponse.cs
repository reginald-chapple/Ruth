namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Time-bounded RustFS presigned download link for a settled digital order line.</summary>
/// <param name="OrderId">Order identifier.</param>
/// <param name="LineId">Order line identifier.</param>
/// <param name="BinaryFileId">Core BinaryFile identifier.</param>
/// <param name="DownloadUrl">Presigned download URL.</param>
/// <param name="ExpiresAtUtc">UTC expiration timestamp.</param>
public sealed record OrderDigitalDownloadLinkResponse(
    Guid OrderId,
    Guid LineId,
    Guid BinaryFileId,
    string DownloadUrl,
    DateTimeOffset ExpiresAtUtc
);
