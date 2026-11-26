namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Read projection representing a configured financial payment gateway.</summary>
/// <param name="Id">Unique gateway configuration identifier.</param>
/// <param name="Name">Gateway display name.</param>
/// <param name="ProviderKey">Bound payment gateway provider key.</param>
/// <param name="ProviderAvailable">Whether a matching payment gateway provider is registered in DI.</param>
/// <param name="Description">Optional description.</param>
/// <param name="SupportsCards">Whether card payments are enabled.</param>
/// <param name="SupportsAch">Whether ACH payments are enabled.</param>
/// <param name="SupportsRecurring">Whether recurring billing is enabled.</param>
/// <param name="IsTestMode">Whether sandbox/test mode is enabled.</param>
/// <param name="IsActive">Whether the gateway is active.</param>
/// <param name="BatchTimeOffsetHours">Settlement cutoff hour offset (-12..14).</param>
/// <param name="PublicTokenizerKey">Optional client-side hosted tokenizer public key.</param>
/// <param name="Settings">Non-secret gateway configuration map.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record FinancialGatewayResponse(
    Guid Id,
    string Name,
    string ProviderKey,
    bool ProviderAvailable,
    string? Description,
    bool SupportsCards,
    bool SupportsAch,
    bool SupportsRecurring,
    bool IsTestMode,
    bool IsActive,
    int BatchTimeOffsetHours,
    string? PublicTokenizerKey,
    IReadOnlyDictionary<string, string> Settings,
    DateTimeOffset CreatedOnUtc
);
