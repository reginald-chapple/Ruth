namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Line item representing a net settled tax-deductible charitable contribution or grant gift.</summary>
public sealed record StatementContributionLineDto(
    Guid TransactionId,
    DateTimeOffset TransactionDateUtc,
    string AccountName,
    string AccountCode,
    string TenderLabel,
    string? ReferenceCode,
    decimal NetDeductibleAmount,
    string CurrencyCode
);
