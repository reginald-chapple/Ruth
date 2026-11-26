using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Contracts.Events;

/// <summary>Outbox integration event emitted when a financial transaction is settled or recorded.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when recorded.</param>
/// <param name="TransactionId">Recorded financial transaction identifier.</param>
/// <param name="AuthorizedPersonId">Optional constituent or organization Person identifier.</param>
/// <param name="TotalAmount">Total transaction monetary amount.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="TaxDeductibleAmount">Portion of the transaction allocated to tax-deductible accounts.</param>
/// <param name="NonDeductibleAmount">Portion allocated to non-deductible dues, fees, or operating revenue.</param>
/// <param name="SourceType">Originating channel.</param>
/// <param name="TenderType">Payment tender instrument.</param>
public sealed record FinancialTransactionRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid TransactionId,
    Guid? AuthorizedPersonId,
    decimal TotalAmount,
    string CurrencyCode,
    decimal TaxDeductibleAmount,
    decimal NonDeductibleAmount,
    FinancialTransactionSourceType SourceType,
    FinancialTenderType TenderType
) : IIntegrationEvent;
