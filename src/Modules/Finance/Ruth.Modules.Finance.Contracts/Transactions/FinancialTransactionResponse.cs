namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Complete read projection for a financial transaction, its multi-account splits, encrypted check images, and refunds.</summary>
/// <param name="Id">Unique transaction identifier.</param>
/// <param name="TransactionCode">Gateway transaction ID or check number.</param>
/// <param name="AuthorizedPersonId">Optional constituent or Business organization Person identifier.</param>
/// <param name="HouseholdId">Optional Household identifier.</param>
/// <param name="BatchId">Optional parent financial batch identifier.</param>
/// <param name="FinancialGatewayId">Optional gateway configuration identifier.</param>
/// <param name="SavedPaymentMethodId">Optional saved payment method identifier.</param>
/// <param name="RecurringScheduleId">Optional originating recurring schedule identifier.</param>
/// <param name="TransactionDateTimeUtc">Effective transaction UTC timestamp.</param>
/// <param name="SourceType">Originating channel.</param>
/// <param name="TenderType">Payment tender instrument.</param>
/// <param name="Status">Current settlement or refund status.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="TotalAmount">Total gross transaction amount.</param>
/// <param name="RefundedAmount">Cumulative refunded amount.</param>
/// <param name="NetAmount">Net retained amount (TotalAmount minus RefundedAmount).</param>
/// <param name="Summary">Optional transaction memo.</param>
/// <param name="Splits">Multi-account allocation splits.</param>
/// <param name="Images">Encrypted check/remittance image metadata.</param>
/// <param name="Refunds">Applied refund records.</param>
public sealed record FinancialTransactionResponse(
    Guid Id,
    string? TransactionCode,
    Guid? AuthorizedPersonId,
    Guid? HouseholdId,
    Guid? BatchId,
    Guid? FinancialGatewayId,
    Guid? SavedPaymentMethodId,
    Guid? RecurringScheduleId,
    DateTimeOffset TransactionDateTimeUtc,
    FinancialTransactionSourceType SourceType,
    FinancialTenderType TenderType,
    FinancialTransactionStatus Status,
    string CurrencyCode,
    decimal TotalAmount,
    decimal RefundedAmount,
    decimal NetAmount,
    string? Summary,
    IReadOnlyList<FinancialTransactionSplitResponse> Splits,
    IReadOnlyList<FinancialTransactionImageResponse> Images,
    IReadOnlyList<FinancialTransactionRefundResponse> Refunds
);
