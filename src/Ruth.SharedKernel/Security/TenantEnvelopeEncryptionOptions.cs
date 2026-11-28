namespace Ruth.SharedKernel.Security;

/// <summary>
/// Configuration options for per-tenant AES-256-GCM envelope encryption of third-party credentials at rest.
/// </summary>
public sealed class TenantEnvelopeEncryptionOptions
{
    /// <summary>Configuration section path for credential envelope encryption.</summary>
    public const string SectionName = "Security:CredentialEncryption";

    /// <summary>Default local-development/test master KEK material when none is configured.</summary>
    public const string DevelopmentDefaultMasterKey =
        "ruth-dev-tenant-envelope-master-kek-change-in-production-2026";

    /// <summary>Minimum character length required for explicitly configured production KEK secrets.</summary>
    public const int MinimumKeyMaterialLength = 32;

    /// <summary>Gets or sets the identifier of the active Key Encryption Key (KEK) used for new envelopes.</summary>
    public string ActiveKeyId { get; set; } = "kek-v1";

    /// <summary>Gets or sets the active master Key Encryption Key material.</summary>
    public string MasterKey { get; set; } = DevelopmentDefaultMasterKey;

    /// <summary>
    /// Gets or sets historical Key Encryption Keys retained for decrypting envelopes written prior to KEK rotation.
    /// </summary>
    public IDictionary<string, string> PreviousKeys { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
