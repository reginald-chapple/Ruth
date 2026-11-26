namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Supported mobile wallet pass packaging platforms.</summary>
public enum WalletPassPlatform
{
    /// <summary>Apple Wallet signed .pkpass archive bundle.</summary>
    ApplePkpass = 0,

    /// <summary>Google Wallet RS256 signed Save JWT link.</summary>
    GoogleWallet = 1,

    /// <summary>Deterministic SVG and JSON preview card when platform signing keys are unconfigured.</summary>
    PreviewCard = 2,
}
