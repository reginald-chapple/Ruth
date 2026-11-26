using Microsoft.Extensions.Options;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure.WalletPasses;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class WalletPassBundleComposer(
    IEnumerable<IWalletPassProvider> providers,
    IOptionsMonitor<WalletPassSecurityOptions> securityOptions
)
{
    public WalletPassSecurityOptions SecurityOptions => securityOptions.CurrentValue;

    public IReadOnlyList<WalletPassProviderStatusDto> GetProviderStatuses()
    {
        return
        [
            .. providers
                .OrderBy(static p => p.Platform)
                .Select(static p => new WalletPassProviderStatusDto(
                    p.ProviderKey,
                    p.Platform,
                    p.IsConfigured
                )),
        ];
    }

    public async Task<WalletPassClaimBundleResponse> BuildClaimBundleAsync(
        IssuedWalletPass pass,
        WalletPassTemplate template,
        int deviceCount,
        string? publicBaseUrlOverride,
        CancellationToken cancellationToken
    )
    {
        var opts = securityOptions.CurrentValue;
        var baseUrl = string.IsNullOrWhiteSpace(publicBaseUrlOverride)
            ? opts.PublicBaseUrl
            : publicBaseUrlOverride.Trim();
        var rawAppleAuth = WalletPassSecurityTokens.DeriveAppleAuthToken(
            opts.PassKitHmacSecret,
            pass.Id.Value,
            pass.AuthTokenSalt
        );

        var spec = WalletPassMapper.ToRenderSpec(pass, template, rawAppleAuth, baseUrl);
        var previewArtifact = await RenderPlatformOrNullAsync(
            WalletPassPlatform.PreviewCard,
            spec,
            cancellationToken
        );
        var appleArtifact = await RenderPlatformOrNullAsync(
            WalletPassPlatform.ApplePkpass,
            spec,
            cancellationToken
        );
        var googleArtifact = await RenderPlatformOrNullAsync(
            WalletPassPlatform.GoogleWallet,
            spec,
            cancellationToken
        );

        return new WalletPassClaimBundleResponse(
            WalletPassMapper.ToResponse(pass, template.Name, deviceCount),
            previewArtifact!,
            appleArtifact,
            googleArtifact
        );
    }

    public async Task<byte[]> BuildPassKitArchiveBytesAsync(
        IssuedWalletPass pass,
        WalletPassTemplate template,
        string rawAppleAuthToken,
        string? publicBaseUrlOverride,
        CancellationToken cancellationToken
    )
    {
        var baseUrl = string.IsNullOrWhiteSpace(publicBaseUrlOverride)
            ? securityOptions.CurrentValue.PublicBaseUrl
            : publicBaseUrlOverride.Trim();
        var spec = WalletPassMapper.ToRenderSpec(pass, template, rawAppleAuthToken, baseUrl);

        var apple = await RenderPlatformOrNullAsync(
            WalletPassPlatform.ApplePkpass,
            spec,
            cancellationToken
        );
        return apple?.BinaryPayload
            ?? PreviewWalletPassProvider.BuildDeterministicUnsignedPkpassZip(spec);
    }

    private async Task<WalletPassArtifactResponse?> RenderPlatformOrNullAsync(
        WalletPassPlatform platform,
        WalletPassRenderSpecDto spec,
        CancellationToken cancellationToken
    )
    {
        var provider = providers.FirstOrDefault(p => p.Platform == platform && p.IsConfigured);
        if (provider is null)
        {
            return null;
        }

        var result = await provider.RenderPassAsync(spec, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }
}
