using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Query to retrieve a single financial statement template by identifier.</summary>
public sealed record GetFinancialStatementTemplateByIdQuery(Guid TemplateId)
    : IQuery<FinancialStatementTemplateResponse>;
