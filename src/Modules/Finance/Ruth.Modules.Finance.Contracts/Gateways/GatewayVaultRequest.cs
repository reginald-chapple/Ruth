namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Request payload to exchange a single-use hosted tokenizer token for a reusable customer vault token.</summary>
/// <param name="PersonId">Constituent or organization Person identifier.</param>
/// <param name="HostedPaymentToken">Single-use payment token produced by the hosted JS tokenizer.</param>
/// <param name="MethodKind">Payment instrument kind.</param>
/// <param name="BillingName">Optional cardholder or account holder name.</param>
/// <param name="BillingEmail">Optional billing email.</param>
/// <param name="MaskedLast4Hint">Optional last-4 digits hint returned by the client tokenizer.</param>
/// <param name="CardBrandHint">Optional card brand hint returned by the client tokenizer.</param>
/// <param name="ExpiryMonthHint">Optional card expiration month (1..12).</param>
/// <param name="ExpiryYearHint">Optional four-digit card expiration year.</param>
/// <param name="IsTestMode">Whether the gateway is operating in sandbox/test mode.</param>
public sealed record GatewayVaultRequest(
    Guid PersonId,
    string HostedPaymentToken,
    PaymentMethodKind MethodKind,
    string? BillingName = null,
    string? BillingEmail = null,
    string? MaskedLast4Hint = null,
    string? CardBrandHint = null,
    int? ExpiryMonthHint = null,
    int? ExpiryYearHint = null,
    bool IsTestMode = false
);
