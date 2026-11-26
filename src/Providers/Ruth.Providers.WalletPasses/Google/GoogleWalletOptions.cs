namespace Ruth.Providers.WalletPasses.Google;

/// <summary>Configuration options for signing Google Wallet Save RS256 JWT credentials.</summary>
public sealed class GoogleWalletOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "WalletPasses:Google";

    /// <summary>Google Wallet Issuer ID prefix (for example 3388000000012345678).</summary>
    public string? IssuerId { get; set; }

    /// <summary>Google Cloud service account client_email.</summary>
    public string? ServiceAccountEmail { get; set; }

    /// <summary>PEM-encoded RSA private key for the Google Wallet service account.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Allowed web origins embedded in the Save to Google Wallet JWT.</summary>
    public IList<string> AllowedOrigins { get; } = ["https://localhost:5080"];
}
