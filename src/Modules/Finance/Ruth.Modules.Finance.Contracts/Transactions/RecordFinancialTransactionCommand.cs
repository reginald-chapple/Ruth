using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Command to record a charitable contribution, fee payment, or invoice settlement with optional live gateway charge.</summary>
/// <param name="AuthorizedPersonId">Optional constituent or organization Person identifier.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="SourceType">Originating channel.</param>
/// <param name="TenderType">Payment tender instrument.</param>
/// <param name="Splits">One or more positive account/invoice splits.</param>
/// <param name="BatchId">Optional open financial batch identifier.</param>
/// <param name="FinancialGatewayId">Optional financial gateway identifier when charging electronically.</param>
/// <param name="SavedPaymentMethodId">Optional vaulted payment method identifier to charge.</param>
/// <param name="OneTimeHostedToken">Optional single-use hosted tokenizer token to charge.</param>
/// <param name="TransactionCode">Optional check number or external reference when not charging a gateway.</param>
/// <param name="TransactionDateTimeUtc">Optional effective transaction UTC timestamp (defaults to UTC now).</param>
/// <param name="HouseholdId">Optional Household identifier.</param>
/// <param name="RecurringScheduleId">Optional recurring schedule identifier.</param>
/// <param name="Summary">Optional transaction memo.</param>
public sealed record RecordFinancialTransactionCommand(
    Guid? AuthorizedPersonId,
    string CurrencyCode,
    FinancialTransactionSourceType SourceType,
    FinancialTenderType TenderType,
    IReadOnlyList<FinancialTransactionSplitSpec> Splits,
    Guid? BatchId = null,
    Guid? FinancialGatewayId = null,
    Guid? SavedPaymentMethodId = null,
    string? OneTimeHostedToken = null,
    string? TransactionCode = null,
    DateTimeOffset? TransactionDateTimeUtc = null,
    Guid? HouseholdId = null,
    Guid? RecurringScheduleId = null,
    string? Summary = null
) : ICommand<FinancialTransactionResponse>;
