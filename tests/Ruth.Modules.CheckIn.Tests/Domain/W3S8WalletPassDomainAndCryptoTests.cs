using System.Buffers.Text;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure.WalletPasses;
using Ruth.Providers.WalletPasses.Apple;
using Ruth.Providers.WalletPasses.Google;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Domain;

public sealed class W3S8WalletPassDomainAndCryptoTests
{
    [Fact]
    public void TemplateValidation_RejectsInvalidInputs_AndProtectsSystemTemplate()
    {
        Assert.True(
            WalletPassTemplate
                .Create(
                    new WalletPassTemplateSpec(
                        "Invalid PassType",
                        "bad-passtype",
                        "org.ruth.notpass",
                        "Ruth",
                        "Desc"
                    )
                )
                .IsFailure
        );

        Assert.True(
            WalletPassTemplate
                .Create(
                    new WalletPassTemplateSpec(
                        "Invalid Color",
                        "bad-color",
                        "pass.org.ruth.member",
                        "Ruth",
                        "Desc",
                        BackgroundColorHex: "blue"
                    )
                )
                .IsFailure
        );

        var template = CreateValidSystemTemplate();
        Assert.True(template.EnsureCanDelete().IsFailure);
    }

    [Fact]
    public void IssuedPass_EnforcesConstantTimeAuth_DynamicUpdates_AndRevocation()
    {
        var template = CreateValidSystemTemplate();
        var now = new DateTimeOffset(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);
        var personId = Guid.CreateVersion7();
        const string secret = "unit-test-hmac-secret-2026";

        var (pass, rawClaim) = IssuedWalletPass
            .Issue(
                template,
                personId,
                new WalletPassDynamicFieldsDto(
                    HolderDisplayName: "Alex Rivera",
                    MembershipTier: "Gold",
                    PunchOrCreditBalance: 8,
                    CustomFields: [new WalletPassCustomFieldDto("branch", "BRANCH", "Downtown")]
                ),
                secret,
                now
            )
            .Value;

        Assert.True(
            WalletPassSecurityTokens.ConstantTimeMatchesHash(rawClaim, pass.ClaimTokenHash)
        );
        Assert.False(
            WalletPassSecurityTokens.ConstantTimeMatchesHash("wrong-token", pass.ClaimTokenHash)
        );

        var expectedAppleToken = WalletPassSecurityTokens.DeriveAppleAuthToken(
            secret,
            pass.Id.Value,
            pass.AuthTokenSalt
        );
        Assert.True(
            pass.VerifyAppleAuthToken(
                WalletPassSecurityTokens.ExtractApplePassHeaderToken(
                    $"ApplePass {expectedAppleToken}"
                )
            )
        );
        Assert.False(pass.VerifyAppleAuthToken("00000000000000000000000000000000"));

        var initialSeq = pass.UpdatedSequence;
        Assert.True(
            pass.UpdateDynamicFields(
                new WalletPassDynamicFieldsDto(PunchOrCreditBalance: -1),
                expiresAtUtc: null,
                now.AddMinutes(5)
            ).IsFailure
        );

        Assert.True(
            pass.UpdateDynamicFields(
                new WalletPassDynamicFieldsDto(MembershipTier: "Platinum", PunchOrCreditBalance: 7),
                expiresAtUtc: null,
                now.AddMinutes(5)
            ).IsSuccess
        );
        Assert.Equal(2, pass.Revision);
        Assert.True(pass.UpdatedSequence > initialSeq);

        var mergedId = Guid.CreateVersion7();
        pass.RepointPerson(personId, mergedId);
        Assert.Equal(mergedId, pass.PersonId);

        _ = pass.Revoke("Lost phone", now.AddMinutes(10));
        Assert.Equal(IssuedWalletPassStatus.Revoked, pass.Status);
        Assert.True(pass.EnsureActiveForScan(now.AddMinutes(11)).IsFailure);
    }

    [Fact]
    public async Task ApplePkpassProvider_SignsValidDetachedPkcs7CmsArchive_AndDetectsManifestTampering()
    {
        using var signerKey = RSA.Create(2048);
        var certReq = new CertificateRequest(
            "CN=Pass Type ID: pass.org.ruth.member, O=Ruth Test",
            signerKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        using var signerCert = certReq.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1)
        );

        var options = new FixedOptionsMonitor<ApplePkpassOptions>(
            new ApplePkpassOptions
            {
                TeamIdentifier = "RUTH123456",
                PassCertificatePem = signerCert.ExportCertificatePem(),
                PassPrivateKeyPem = signerKey.ExportPkcs8PrivateKeyPem(),
            }
        );

        var provider = new ApplePkpassProvider(options, new SystemClock());
        Assert.True(provider.IsConfigured);

        var artifact = (await provider.RenderPassAsync(CreateSampleSpec())).Value;
        Assert.True(artifact.IsSignedProductionPackage);
        Assert.Equal("application/vnd.apple.pkpass", artifact.ContentType);
        Assert.NotNull(artifact.BinaryPayload);

        await using var zipStream = new MemoryStream(artifact.BinaryPayload);
        await using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var manifestBytes = ReadAllEntryBytes(zip, "manifest.json");
        var signatureBytes = ReadAllEntryBytes(zip, "signature");
        var passJsonBytes = ReadAllEntryBytes(zip, "pass.json");

        var manifestMap = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestBytes)!;
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(passJsonBytes)),
            manifestMap["pass.json"]
        );

        Assert.True(Pkcs7DetachedCmsBuilder.VerifyDetached(manifestBytes, signatureBytes));

        var tamperedManifest = manifestBytes.ToArray();
        tamperedManifest[0] ^= 0xFF;
        Assert.False(Pkcs7DetachedCmsBuilder.VerifyDetached(tamperedManifest, signatureBytes));
    }

    [Fact]
    public async Task GoogleWalletJwtProvider_SignsVerifiableRs256SaveUrl_AndPreviewEscapesHtml()
    {
        using var rsa = RSA.Create(2048);
        var googleOpts = new GoogleWalletOptions
        {
            IssuerId = "3388000000099999999",
            ServiceAccountEmail = "wallet-signer@ruth-test.iam.gserviceaccount.com",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
        };

        var googleProvider = new GoogleWalletJwtProvider(
            new FixedOptionsMonitor<GoogleWalletOptions>(googleOpts),
            new SystemClock()
        );
        Assert.True(googleProvider.IsConfigured);

        var spec = CreateSampleSpec();
        var googleArtifact = (await googleProvider.RenderPassAsync(spec)).Value;
        Assert.NotNull(googleArtifact.SaveUrlOrJwt);
        Assert.StartsWith(
            "https://pay.google.com/gp/v/save/",
            googleArtifact.SaveUrlOrJwt,
            StringComparison.Ordinal
        );

        var jwt = googleArtifact.SaveUrlOrJwt["https://pay.google.com/gp/v/save/".Length..];
        var parts = jwt.Split('.');
        Assert.Equal(3, parts.Length);

        var signedInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        var sigBytes = Base64Url.DecodeFromChars(parts[2]);
        Assert.True(
            rsa.VerifyData(
                signedInput,
                sigBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            )
        );

        var payloadJson = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1]));
        Assert.Contains("WLT-ABCD1234EF567890", payloadJson, StringComparison.Ordinal);

        var previewProvider = new PreviewWalletPassProvider();
        var preview = (await previewProvider.RenderPassAsync(spec)).Value;
        Assert.DoesNotContain(
            "<script>",
            preview.PreviewSvgMarkup,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.Contains("&lt;script&gt;", preview.PreviewSvgMarkup, StringComparison.Ordinal);
    }

    private static WalletPassTemplate CreateValidSystemTemplate()
    {
        return WalletPassTemplate
            .Create(
                new WalletPassTemplateSpec(
                    "Member Credential",
                    "member-credential",
                    "pass.org.ruth.member",
                    "Ruth Community",
                    "Digital member credential",
                    DefaultValidityDays: 180,
                    IsSystem: true
                )
            )
            .Value;
    }

    private static WalletPassRenderSpecDto CreateSampleSpec()
    {
        return new WalletPassRenderSpecDto(
            Guid.CreateVersion7(),
            "RWP-1122334455667788",
            "WLT-ABCD1234EF567890",
            "pass.org.ruth.member",
            "Ruth Community",
            "Standard Member Pass",
            "Member pass description",
            WalletPassCardStyle.PunchOrCreditCard,
            "#1E3A8A",
            "#FFFFFF",
            "#BFDBFE",
            "Ruth",
            "https://passes.ruth.org",
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(90),
            Revision: 1,
            new WalletPassDynamicFieldsDto(
                HolderDisplayName: "Casey <script> Jordan",
                MembershipTier: "Member",
                PunchOrCreditBalance: 5,
                NextShiftOrBookingSummary: "Studio B - Tue 6 PM"
            )
        );
    }

    private static byte[] ReadAllEntryBytes(ZipArchive zip, string entryName)
    {
        var entry = zip.GetEntry(entryName);
        Assert.NotNull(entry);
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private sealed class FixedOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue => currentValue;

        public T Get(string? name)
        {
            return currentValue;
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }
}
