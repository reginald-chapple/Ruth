using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Infrastructure.WalletPasses;

/// <summary>Deterministic fallback wallet pass provider used whenever Apple/Google signing certificates are unconfigured.</summary>
public sealed class PreviewWalletPassProvider : IWalletPassProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <inheritdoc />
    public string ProviderKey => "preview";

    /// <inheritdoc />
    public WalletPassPlatform Platform => WalletPassPlatform.PreviewCard;

    /// <inheritdoc />
    public bool IsConfigured => true;

    /// <inheritdoc />
    public Task<Result<WalletPassArtifactResponse>> RenderPassAsync(
        WalletPassRenderSpecDto spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var svg = WalletPassSvgCardRenderer.RenderSvg(spec);
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                format = "ruth.wallet.preview.v1",
                spec.SerialNumber,
                spec.ScanToken,
                spec.PassTypeIdentifier,
                spec.OrganizationName,
                spec.TemplateName,
                cardStyle = spec.CardStyle.ToString(),
                spec.Revision,
                spec.IssuedAtUtc,
                spec.ExpiresAtUtc,
                spec.DynamicFields,
            },
            _jsonOptions
        );

        return Task.FromResult(
            Result.Success(
                new WalletPassArtifactResponse(
                    ProviderKey,
                    WalletPassPlatform.PreviewCard,
                    IsSignedProductionPackage: false,
                    "application/json",
                    $"ruth-pass-{spec.SerialNumber}.preview.json",
                    jsonBytes,
                    SaveUrlOrJwt: $"/api/checkin/wallet-passes/preview/{spec.IssuedPassId:D}/svg",
                    svg
                )
            )
        );
    }

    /// <summary>Builds a deterministic unsigned .pkpass zip archive for local PassKit callback testing when Apple certificates are absent.</summary>
    internal static byte[] BuildDeterministicUnsignedPkpassZip(WalletPassRenderSpecDto spec)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var passJson = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    formatVersion = 1,
                    passTypeIdentifier = spec.PassTypeIdentifier,
                    serialNumber = spec.SerialNumber,
                    organizationName = spec.OrganizationName,
                    description = spec.Description,
                    authenticationToken = spec.AppleAuthenticationToken,
                    webServiceURL = spec.WebServiceBaseUrl,
                    barcode = new
                    {
                        format = "PKBarcodeFormatQR",
                        message = spec.ScanToken,
                        messageEncoding = "iso-8859-1",
                    },
                },
                _jsonOptions
            );
            WriteEntry(zip, "pass.json", passJson);
            WriteEntry(
                zip,
                "preview.svg",
                Encoding.UTF8.GetBytes(WalletPassSvgCardRenderer.RenderSvg(spec))
            );
        }

        return ms.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string entryName, byte[] bytes)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes);
    }
}
