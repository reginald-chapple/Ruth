using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.WalletPasses.Apple;

/// <summary>Apple Wallet .pkpass provider packaging pass.json, PNG icons, SHA-1 manifest.json, and detached PKCS#7 CMS signature.</summary>
public sealed class ApplePkpassProvider(
    IOptionsMonitor<ApplePkpassOptions> optionsMonitor,
    IClock clock
) : IWalletPassProvider
{
    /// <inheritdoc />
    public string ProviderKey => "apple-pkpass";

    /// <inheritdoc />
    public WalletPassPlatform Platform => WalletPassPlatform.ApplePkpass;

    /// <inheritdoc />
    public bool IsConfigured => HasValidSigningMaterial(optionsMonitor.CurrentValue);

    /// <inheritdoc />
    public Task<Result<WalletPassArtifactResponse>> RenderPassAsync(
        WalletPassRenderSpecDto spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var opts = optionsMonitor.CurrentValue;
        if (!HasValidSigningMaterial(opts))
        {
            return Task.FromResult(
                Result.Failure<WalletPassArtifactResponse>(
                    DomainError.Validation(
                        "WalletPass.AppleNotConfigured",
                        "Apple PassKit TeamIdentifier and X.509 signing certificate are not configured."
                    )
                )
            );
        }

        using var material = LoadSigningMaterial(opts);
        var zipBytes = BuildSignedPkpassArchive(
            spec,
            opts.TeamIdentifier!.Trim(),
            material.SignerCert,
            material.SignerKey,
            material.WwdrCert,
            clock.UtcNow
        );

        return Task.FromResult(
            Result.Success(
                new WalletPassArtifactResponse(
                    ProviderKey,
                    WalletPassPlatform.ApplePkpass,
                    IsSignedProductionPackage: true,
                    "application/vnd.apple.pkpass",
                    $"ruth-pass-{spec.SerialNumber}.pkpass",
                    zipBytes,
                    SaveUrlOrJwt: null,
                    WalletPassSvgCardRenderer.RenderSvg(spec)
                )
            )
        );
    }

    internal static byte[] BuildSignedPkpassArchive(
        WalletPassRenderSpecDto spec,
        string teamIdentifier,
        X509Certificate2 signerCert,
        RSA signerKey,
        X509Certificate2? wwdrCert,
        DateTimeOffset signingTimeUtc
    )
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["pass.json"] = ApplePassJsonAndPngAssets.BuildPassJsonBytes(spec, teamIdentifier),
            ["icon.png"] = ApplePassJsonAndPngAssets.GetIconPng(),
            ["icon@2x.png"] = ApplePassJsonAndPngAssets.GetIconPng(),
            ["logo.png"] = ApplePassJsonAndPngAssets.GetIconPng(),
        };

        var manifestMap = files.ToDictionary(
            static kvp => kvp.Key,
            static kvp => Convert.ToHexStringLower(SHA256.HashData(kvp.Value)),
            StringComparer.Ordinal
        );
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifestMap);
        var signatureBytes = Pkcs7DetachedCmsBuilder.SignDetached(
            manifestBytes,
            signerCert,
            signerKey,
            wwdrCert,
            signingTimeUtc
        );

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var kvp in files)
            {
                WriteZipEntry(zip, kvp.Key, kvp.Value);
            }

            WriteZipEntry(zip, "manifest.json", manifestBytes);
            WriteZipEntry(zip, "signature", signatureBytes);
        }

        return ms.ToArray();
    }

    private static void WriteZipEntry(ZipArchive zip, string name, byte[] content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content);
    }

    private static bool HasValidSigningMaterial(ApplePkpassOptions opts)
    {
        return !string.IsNullOrWhiteSpace(opts.TeamIdentifier)
            && (
                !string.IsNullOrWhiteSpace(opts.PassCertificatePkcs12Base64)
                || (
                    !string.IsNullOrWhiteSpace(opts.PassCertificatePem)
                    && !string.IsNullOrWhiteSpace(opts.PassPrivateKeyPem)
                )
            );
    }

    private static SigningMaterial LoadSigningMaterial(ApplePkpassOptions opts)
    {
        X509Certificate2 cert;
        RSA key;

        if (!string.IsNullOrWhiteSpace(opts.PassCertificatePem))
        {
            cert = X509Certificate2.CreateFromPem(opts.PassCertificatePem);
            key = RSA.Create();
            key.ImportFromPem(opts.PassPrivateKeyPem);
        }
        else
        {
            var rawPfx = Convert.FromBase64String(opts.PassCertificatePkcs12Base64!.Trim());
            cert = X509CertificateLoader.LoadPkcs12(
                rawPfx,
                opts.PassCertificatePassword,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet
            );
            key = cert.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("Apple PassKit certificate lacks RSA key.");
        }

        var wwdr = string.IsNullOrWhiteSpace(opts.WwdrIntermediateCertificatePem)
            ? null
            : X509Certificate2.CreateFromPem(opts.WwdrIntermediateCertificatePem);

        return new SigningMaterial(cert, key, wwdr);
    }

    private sealed record SigningMaterial(
        X509Certificate2 SignerCert,
        RSA SignerKey,
        X509Certificate2? WwdrCert
    ) : IDisposable
    {
        public void Dispose()
        {
            WwdrCert?.Dispose();
            SignerKey.Dispose();
            SignerCert.Dispose();
        }
    }
}
