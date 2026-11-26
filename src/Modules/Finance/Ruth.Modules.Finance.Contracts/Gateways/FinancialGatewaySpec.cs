namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Specification payload for configuring a financial payment gateway.</summary>
/// <param name="Name">Staff display name for this gateway profile.</param>
/// <param name="ProviderKey">Registered payment gateway provider key (for example, test, nmi, stripe).</param>
/// <param name="Description">Optional gateway description.</param>
/// <param name="SupportsCards">Whether credit/debit cards are enabled on this gateway.</param>
/// <param name="SupportsAch">Whether ACH bank drafts are enabled on this gateway.</param>
/// <param name="SupportsRecurring">Whether recurring subscriptions are enabled on this gateway.</param>
/// <param name="IsTestMode">Whether transactions run in sandbox/test mode.</param>
/// <param name="IsActive">Whether this gateway is active for processing.</param>
/// <param name="BatchTimeOffsetHours">Settlement batch cutoff hour offset from UTC (-12..14).</param>
/// <param name="PublicTokenizerKey">Optional public client-side hosted tokenizer key.</param>
/// <param name="Settings">Optional non-secret merchant configuration key/values.</param>
public sealed record FinancialGatewaySpec(
    string Name,
    string ProviderKey,
    string? Description = null,
    bool SupportsCards = true,
    bool SupportsAch = true,
    bool SupportsRecurring = true,
    bool IsTestMode = false,
    bool IsActive = true,
    int BatchTimeOffsetHours = 0,
    string? PublicTokenizerKey = null,
    IReadOnlyDictionary<string, string>? Settings = null
);
