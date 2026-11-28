using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Per-tenant AAD-bound AES-256-GCM envelope encryption contract protecting database-stored third-party secrets at rest.
/// </summary>
public interface ITenantCredentialProtector
{
    /// <summary>Gets the identifier of the currently active Key Encryption Key (KEK).</summary>
    string ActiveKeyId { get; }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> using a fresh 256-bit Data Encryption Key (DEK) wrapped under the active KEK,
    /// binding <paramref name="tenantId" /> and <paramref name="purpose" /> into AES-256-GCM associated authenticated data.
    /// </summary>
    string Protect(TenantId tenantId, string purpose, string plaintext);

    /// <summary>
    /// Unwraps and decrypts an envelope ciphertext, verifying both <paramref name="tenantId" /> and <paramref name="purpose" /> AAD.
    /// Throws <see cref="System.Security.Cryptography.CryptographicException" /> on any cross-tenant, cross-purpose, or tamper mismatch.
    /// </summary>
    string Unprotect(TenantId tenantId, string purpose, string envelopeCiphertext);

    /// <summary>
    /// Attempts to unwrap and decrypt an envelope ciphertext without throwing on cryptographic verification failure.
    /// </summary>
    bool TryUnprotect(
        TenantId tenantId,
        string purpose,
        string? envelopeCiphertext,
        out string plaintext
    );

    /// <summary>Returns true when <paramref name="candidate" /> carries the canonical envelope prefix.</summary>
    bool IsProtectedEnvelope(string? candidate);
}
