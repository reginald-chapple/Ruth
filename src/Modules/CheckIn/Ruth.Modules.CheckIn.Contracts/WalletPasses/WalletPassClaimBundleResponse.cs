namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Multi-platform wallet pass download and visual preview bundle returned for a claim token or staff preview.</summary>
/// <param name="Pass">Issued wallet pass summary.</param>
/// <param name="PreviewCard">Always-available deterministic SVG and JSON preview card.</param>
/// <param name="ApplePkpass">Signed Apple Wallet .pkpass artifact when Apple certificates are configured.</param>
/// <param name="GoogleWallet">Signed Google Wallet RS256 Save JWT artifact when Google credentials are configured.</param>
public sealed record WalletPassClaimBundleResponse(
    IssuedWalletPassResponse Pass,
    WalletPassArtifactResponse PreviewCard,
    WalletPassArtifactResponse? ApplePkpass,
    WalletPassArtifactResponse? GoogleWallet
);
