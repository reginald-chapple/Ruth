using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Resolves a member pass claim token into downloadable Apple .pkpass, Google Wallet JWT, and visual preview artifacts.</summary>
/// <param name="ClaimToken">URL-safe claim token issued with the pass.</param>
/// <param name="PublicBaseUrl">Optional public base URL used to embed PassKit webServiceURL callbacks.</param>
public sealed record ClaimWalletPassQuery(string ClaimToken, string? PublicBaseUrl = null)
    : IQuery<WalletPassClaimBundleResponse>;
