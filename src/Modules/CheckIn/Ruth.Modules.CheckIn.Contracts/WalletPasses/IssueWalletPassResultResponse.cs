namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Outcome of issuing a smart wallet pass, including its one-time/signed claim link and provider availability.</summary>
/// <param name="Pass">Issued wallet pass summary.</param>
/// <param name="ClaimToken">URL-safe claim token whose SHA-256 hash is stored on the pass.</param>
/// <param name="ClaimUrl">Relative or absolute claim URL for downloading Apple/Google/Preview packages.</param>
/// <param name="NotificationQueued">True when an email/SMS claim link notification was queued via Outbox.</param>
/// <param name="Providers">Registered wallet pass providers and their signing readiness.</param>
public sealed record IssueWalletPassResultResponse(
    IssuedWalletPassResponse Pass,
    string ClaimToken,
    string ClaimUrl,
    bool NotificationQueued,
    IReadOnlyList<WalletPassProviderStatusDto> Providers
);
