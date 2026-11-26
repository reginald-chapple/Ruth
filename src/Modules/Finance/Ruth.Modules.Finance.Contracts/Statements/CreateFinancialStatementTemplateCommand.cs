using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Command to create a financial statement or tax receipt template.</summary>
public sealed record CreateFinancialStatementTemplateCommand(FinancialStatementTemplateSpec Spec)
    : ICommand<FinancialStatementTemplateResponse>;
