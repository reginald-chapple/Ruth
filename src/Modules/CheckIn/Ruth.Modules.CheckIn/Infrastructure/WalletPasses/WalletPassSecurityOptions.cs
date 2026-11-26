namespace Ruth.Modules.CheckIn.Infrastructure.WalletPasses;

/// <summary>Configuration options for smart wallet pass token derivation and PassKit callback URLs.</summary>
public sealed class WalletPassSecurityOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "CheckIn:WalletPasses";

    /// <summary>Server-side HMAC-SHA256 secret used to derive PassKit authentication tokens without storing plaintext tokens.</summary>
    public string PassKitHmacSecret { get; set; } =
        "ruth-wallet-passkit-default-hmac-key-2026-override-in-production";

    /// <summary>Optional public HTTPS origin used for PassKit webServiceURL and email/SMS claim links.</summary>
    public string PublicBaseUrl { get; set; } = "https://localhost:5080";
}
