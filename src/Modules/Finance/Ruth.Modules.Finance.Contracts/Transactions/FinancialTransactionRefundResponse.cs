namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Read projection for a full or partial refund applied to a financial transaction.</summary>
/// <param name="Id">Refund record identifier.</param>
/// <param name="Amount">Refunded monetary amount.</param>
/// <param name="Reason">Refund reason.</param>
/// <param name="GatewayRefundReference">Optional external gateway refund transaction ID.</param>
/// <param name="ProcessedByPersonId">Optional staff Person identifier who processed the refund.</param>
/// <param name="RefundedAtUtc">UTC timestamp when refunded.</param>
public sealed record FinancialTransactionRefundResponse(
    Guid Id,
    decimal Amount,
    string Reason,
    string? GatewayRefundReference,
    Guid? ProcessedByPersonId,
    DateTimeOffset RefundedAtUtc
);
