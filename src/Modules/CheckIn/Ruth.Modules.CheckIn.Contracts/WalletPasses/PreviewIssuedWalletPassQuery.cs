using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Renders live Apple, Google, and SVG preview artifacts for an issued wallet pass by its identifier.</summary>
/// <param name="IssuedPassId">Target issued wallet pass identifier.</param>
/// <param name="PublicBaseUrl">Optional public base URL used to embed PassKit webServiceURL callbacks.</param>
public sealed record PreviewIssuedWalletPassQuery(Guid IssuedPassId, string? PublicBaseUrl = null)
    : IQuery<WalletPassClaimBundleResponse>;
