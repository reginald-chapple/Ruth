namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Result returned after executing a refund on a payment gateway.</summary>
/// <param name="GatewayRefundId">External gateway refund transaction identifier.</param>
/// <param name="ProcessedAtUtc">UTC timestamp of refund execution.</param>
public sealed record GatewayRefundResult(string GatewayRefundId, DateTimeOffset ProcessedAtUtc);
