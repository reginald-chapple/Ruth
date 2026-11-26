namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Request payload to issue a full or partial refund on a settled gateway transaction.</summary>
/// <param name="OriginalGatewayTransactionId">External gateway reference of the original charge.</param>
/// <param name="Amount">Positive refund amount.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="Reason">Audit reason for the refund.</param>
/// <param name="IsTestMode">Whether the gateway is operating in sandbox/test mode.</param>
public sealed record GatewayRefundRequest(
    string OriginalGatewayTransactionId,
    decimal Amount,
    string CurrencyCode,
    string Reason,
    bool IsTestMode = false
);
