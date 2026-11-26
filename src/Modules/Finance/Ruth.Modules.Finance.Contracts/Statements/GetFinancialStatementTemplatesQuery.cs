using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Query to list configured financial statement and tax receipt templates.</summary>
public sealed record GetFinancialStatementTemplatesQuery(
    FinancialStatementKind? StatementKind = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<FinancialStatementTemplateResponse>>;
