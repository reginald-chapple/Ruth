namespace Ruth.Providers.WalletPasses.Apple;

/// <summary>Configuration options for signing Apple Wallet .pkpass archives and PassKit APNs push updates.</summary>
public sealed class ApplePkpassOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "WalletPasses:Apple";

    /// <summary>10-character Apple Developer Team Identifier.</summary>
    public string? TeamIdentifier { get; set; }

    /// <summary>Base64-encoded PKCS#12 (.p12/.pfx) Pass Type certificate with private key.</summary>
    public string? PassCertificatePkcs12Base64 { get; set; }

    /// <summary>Optional password unlocking <see cref="PassCertificatePkcs12Base64" />.</summary>
    public string? PassCertificatePassword { get; set; }

    /// <summary>Optional PEM-encoded Pass Type X.509 certificate (alternative to PKCS#12).</summary>
    public string? PassCertificatePem { get; set; }

    /// <summary>Optional PEM-encoded RSA private key paired with <see cref="PassCertificatePem" />.</summary>
    public string? PassPrivateKeyPem { get; set; }

    /// <summary>Optional PEM-encoded Apple WWDR intermediate certificate included in the PKCS#7 certificate chain.</summary>
    public string? WwdrIntermediateCertificatePem { get; set; }
}
