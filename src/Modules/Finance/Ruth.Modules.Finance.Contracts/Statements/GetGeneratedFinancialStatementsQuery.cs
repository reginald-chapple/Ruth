using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Query to list previously generated tax receipts and account statements.</summary>
public sealed record GetGeneratedFinancialStatementsQuery(
    Guid? TargetPersonId = null,
    Guid? TargetHouseholdId = null,
    Guid? BatchRunId = null,
    FinancialStatementKind? StatementKind = null,
    int Limit = 100
) : IQuery<IReadOnlyList<GeneratedFinancialStatementResponse>>;
