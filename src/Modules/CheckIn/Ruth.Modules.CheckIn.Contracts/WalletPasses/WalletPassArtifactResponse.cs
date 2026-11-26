namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Rendered wallet pass package artifact produced by an <see cref="IWalletPassProvider" />.</summary>
/// <param name="ProviderKey">Provider key that produced the artifact.</param>
/// <param name="Platform">Target wallet platform.</param>
/// <param name="IsSignedProductionPackage">True when signed with configured X.509 / RSA credentials.</param>
/// <param name="ContentType">MIME content type for HTTP downloads.</param>
/// <param name="FileName">Suggested download filename.</param>
/// <param name="BinaryPayload">Binary archive or UTF-8 JSON bytes when applicable.</param>
/// <param name="SaveUrlOrJwt">Signed Google Wallet Save URL or preview token when applicable.</param>
/// <param name="PreviewSvgMarkup">Self-contained SVG preview of the rendered wallet pass.</param>
public sealed record WalletPassArtifactResponse(
    string ProviderKey,
    WalletPassPlatform Platform,
    bool IsSignedProductionPackage,
    string ContentType,
    string FileName,
    byte[]? BinaryPayload,
    string? SaveUrlOrJwt,
    string PreviewSvgMarkup
);
