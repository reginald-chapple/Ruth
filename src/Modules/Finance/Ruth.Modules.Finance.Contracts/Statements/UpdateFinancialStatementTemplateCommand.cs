using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Command to update an existing financial statement or tax receipt template.</summary>
public sealed record UpdateFinancialStatementTemplateCommand(
    Guid TemplateId,
    FinancialStatementTemplateSpec Spec
) : ICommand<FinancialStatementTemplateResponse>;
