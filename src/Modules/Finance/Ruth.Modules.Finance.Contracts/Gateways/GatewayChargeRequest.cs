namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Request payload to authorize and capture a card or ACH charge via a payment gateway.</summary>
/// <param name="Amount">Positive decimal charge amount.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="CustomerVaultToken">Optional saved customer vault token.</param>
/// <param name="OneTimeHostedToken">Optional single-use hosted tokenizer token when not charging a saved vault method.</param>
/// <param name="MethodKind">Payment instrument kind.</param>
/// <param name="IdempotencyKey">Idempotency key protecting against duplicate charges.</param>
/// <param name="Description">Statement or audit description.</param>
/// <param name="IsTestMode">Whether the gateway is operating in sandbox/test mode.</param>
public sealed record GatewayChargeRequest(
    decimal Amount,
    string CurrencyCode,
    string? CustomerVaultToken,
    string? OneTimeHostedToken,
    PaymentMethodKind MethodKind,
    string IdempotencyKey,
    string? Description = null,
    bool IsTestMode = false
);
