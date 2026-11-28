using System.Buffers.Text;
using System.Security.Cryptography;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.SharedKernel.Tests.Security;

public sealed class W8S4EnvelopeCryptoAndRedactorTests
{
    private static readonly TenantId _tenantA = TenantId.Create();

    private static readonly TenantId _tenantB = TenantId.Create();

    [Fact]
    public void ProtectAndUnprotect_RoundTripsPlaintext_WithUniqueRandomDekAndNonces()
    {
        var options = new TenantEnvelopeEncryptionOptions
        {
            ActiveKeyId = "k2026-q1",
            MasterKey = "unit-test-kek-material-0123456789-abcdef-9876543210",
        };
        var protector = new AesGcmTenantCredentialProtector(options);

        const string secret = "sk_live_51NonprofitStripeCredentialValue_998877";
        var env1 = protector.Protect(_tenantA, "finance.gateway.settings:secretkey", secret);
        var env2 = protector.Protect(_tenantA, "finance.gateway.settings:secretkey", secret);

        Assert.StartsWith("$ruth-env-v1$k2026-q1$", env1, StringComparison.Ordinal);
        Assert.NotEqual(env1, env2, StringComparer.Ordinal);
        Assert.DoesNotContain(secret, env1, StringComparison.Ordinal);
        Assert.True(protector.IsProtectedEnvelope(env1));

        Assert.Equal(
            secret,
            protector.Unprotect(_tenantA, "finance.gateway.settings:secretkey", env1)
        );
        Assert.Equal(
            secret,
            protector.Unprotect(_tenantA, "finance.gateway.settings:secretkey", env2)
        );
    }

    [Fact]
    public void Unprotect_RejectsCrossTenant_CrossPurpose_EmptyTenant_AndTamperedSegments()
    {
        var protector = new AesGcmTenantCredentialProtector(
            new TenantEnvelopeEncryptionOptions
            {
                ActiveKeyId = "k1",
                MasterKey = "master-kek-for-aad-isolation-verification-2026!",
            }
        );

        var envelope = protector.Protect(
            _tenantA,
            "identity.external-auth.client-secret",
            "oidc-client-secret-top-confidential"
        );

        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(_tenantB, "identity.external-auth.client-secret", envelope)
        );
        Assert.False(
            protector.TryUnprotect(
                _tenantB,
                "identity.external-auth.client-secret",
                envelope,
                out var leaked
            )
        );
        Assert.Empty(leaked);

        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(_tenantA, "comms.transport.credentials:apikey", envelope)
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            protector.Protect(TenantId.Empty, "identity.external-auth.client-secret", "secret")
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            protector.Unprotect(TenantId.Empty, "identity.external-auth.client-secret", envelope)
        );

        var tamperedDek = FlipByteInSegment(envelope, segmentIndex: 3);
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(_tenantA, "identity.external-auth.client-secret", tamperedDek)
        );

        var tamperedData = FlipByteInSegment(envelope, segmentIndex: 4);
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(_tenantA, "identity.external-auth.client-secret", tamperedData)
        );

        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(_tenantA, "identity.external-auth.client-secret", "not-an-envelope")
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(
                _tenantA,
                "identity.external-auth.client-secret",
                "$ruth-env-v1$only-two-parts"
            )
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(
                _tenantA,
                "identity.external-auth.client-secret",
                "$ruth-env-v1$k1$!!!bad-base64!!!$!!!"
            )
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(
                _tenantA,
                "identity.external-auth.client-secret",
                "$ruth-env-v1$k1$AAAA$AAAA"
            )
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(
                _tenantA,
                "identity.external-auth.client-secret",
                envelope.Replace("$k1$", "$unknown-key$", StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public void KeyRotation_UnprotectsLegacyKekViaPreviousKeys_AndRewrapsUnderNewActiveKey()
    {
        const string legacyMaster = "legacy-2025-kek-material-with-high-entropy-0001";
        const string rotatedMaster = "rotated-2026-kek-material-with-high-entropy-0002";

        var v1Protector = new AesGcmTenantCredentialProtector(
            new TenantEnvelopeEncryptionOptions { ActiveKeyId = "k2025", MasterKey = legacyMaster }
        );
        var legacyEnvelope = v1Protector.Protect(
            _tenantA,
            "comms.transport.credentials:apikey",
            "SG.RotatedSendGridKeyValue123"
        );

        var rotatedOptions = new TenantEnvelopeEncryptionOptions
        {
            ActiveKeyId = "k2026",
            MasterKey = rotatedMaster,
        };
        rotatedOptions.PreviousKeys["k2025"] = legacyMaster;
        var v2Protector = new AesGcmTenantCredentialProtector(rotatedOptions);

        using var scope = TenantCredentialEncryptionContext.PushScoped(v2Protector);
        var decrypted = TenantCredentialEncryptionContext.Current.Unprotect(
            _tenantA,
            "comms.transport.credentials:apikey",
            legacyEnvelope
        );
        Assert.Equal("SG.RotatedSendGridKeyValue123", decrypted);

        var rewrapped = TenantCredentialEncryptionContext.Current.Protect(
            _tenantA,
            "comms.transport.credentials:apikey",
            decrypted
        );
        Assert.StartsWith("$ruth-env-v1$k2026$", rewrapped, StringComparison.Ordinal);
        Assert.Equal(
            "SG.RotatedSendGridKeyValue123",
            v2Protector.Unprotect(_tenantA, "comms.transport.credentials:apikey", rewrapped)
        );

        var retiredProtector = new AesGcmTenantCredentialProtector(
            new TenantEnvelopeEncryptionOptions { ActiveKeyId = "k2026", MasterKey = rotatedMaster }
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            retiredProtector.Unprotect(
                _tenantA,
                "comms.transport.credentials:apikey",
                legacyEnvelope
            )
        );
    }

    [Fact]
    public void SensitiveSettingRedactor_RedactsSecrets_PreservesPlaceholders_AndAllowsSafeMetadata()
    {
        var existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SecretKey"] = "sk_live_original_secret",
            ["WebhookSigningSecret"] = "whsec_original_signing",
            ["statement_descriptor"] = "GRACE AID",
        };

        var redacted = SensitiveSettingRedactor.RedactDictionary(existing);
        Assert.Equal(SensitiveSettingRedactor.RedactedMask, redacted["SecretKey"]);
        Assert.Equal(SensitiveSettingRedactor.RedactedMask, redacted["WebhookSigningSecret"]);
        Assert.Equal("GRACE AID", redacted["statement_descriptor"]);

        var incomingUpdate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SecretKey"] = SensitiveSettingRedactor.RedactedMask,
            ["WebhookSigningSecret"] = "whsec_rotated_signing",
            ["statement_descriptor"] = "GRACE COMMUNITY",
        };

        var merged = SensitiveSettingRedactor.MergePreservingRedactedSecrets(
            incomingUpdate,
            existing
        );
        Assert.Equal("sk_live_original_secret", merged["SecretKey"]);
        Assert.Equal("whsec_rotated_signing", merged["WebhookSigningSecret"]);
        Assert.Equal("GRACE COMMUNITY", merged["statement_descriptor"]);
    }

    private static string FlipByteInSegment(string envelope, int segmentIndex)
    {
        var parts = envelope.Split('$');
        var raw = Base64Url.DecodeFromChars(parts[segmentIndex]);
        raw[^1] ^= 0xFF;
        parts[segmentIndex] = Base64Url.EncodeToString(raw);
        return string.Join('$', parts);
    }
}
