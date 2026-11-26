namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Summary returned after executing a batch statement/receipt generation run.</summary>
public sealed record FinancialStatementBatchRunResponse(
    Guid BatchRunId,
    Guid TemplateId,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    int GeneratedCount,
    decimal TaxDeductibleAggregateTotal,
    decimal NonDeductibleAggregateTotal,
    IReadOnlyList<GeneratedFinancialStatementResponse> Statements
);
