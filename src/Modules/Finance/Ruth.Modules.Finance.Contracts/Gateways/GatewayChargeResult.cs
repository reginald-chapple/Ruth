namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Result returned after executing a card or ACH charge on a payment gateway.</summary>
/// <param name="GatewayTransactionId">External gateway transaction reference.</param>
/// <param name="AuthorizationCode">Optional processor authorization code.</param>
/// <param name="IsSettledImmediately">True when immediately settled; false when pending ACH clearing.</param>
/// <param name="ProcessedAtUtc">UTC timestamp of gateway execution.</param>
public sealed record GatewayChargeResult(
    string GatewayTransactionId,
    string? AuthorizationCode,
    bool IsSettledImmediately,
    DateTimeOffset ProcessedAtUtc
);
