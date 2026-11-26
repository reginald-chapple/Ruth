using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Strategy contract for rendering Apple Wallet .pkpass bundles, Google Wallet JWTs, or deterministic preview cards.</summary>
public interface IWalletPassProvider
{
    /// <summary>Gets the stable provider key (for example apple-pkpass, google-wallet, preview).</summary>
    string ProviderKey { get; }

    /// <summary>Gets the target mobile wallet platform.</summary>
    WalletPassPlatform Platform { get; }

    /// <summary>Gets a value indicating whether signing certificates/keys are configured and ready.</summary>
    bool IsConfigured { get; }

    /// <summary>Renders a wallet pass package for the given normalized specification.</summary>
    /// <param name="spec">Normalized pass template, identity, barcode, and dynamic field data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<WalletPassArtifactResponse>> RenderPassAsync(
        WalletPassRenderSpecDto spec,
        CancellationToken cancellationToken = default
    );
}
