namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Reports configuration readiness of a registered <see cref="IWalletPassProvider" />.</summary>
/// <param name="ProviderKey">Provider registration key.</param>
/// <param name="Platform">Target wallet platform.</param>
/// <param name="IsConfigured">True when signing keys/certificates are configured.</param>
public sealed record WalletPassProviderStatusDto(
    string ProviderKey,
    WalletPassPlatform Platform,
    bool IsConfigured
);
