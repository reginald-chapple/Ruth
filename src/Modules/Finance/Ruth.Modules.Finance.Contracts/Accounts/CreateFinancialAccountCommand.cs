using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Command to create a new hierarchical financial account or fund.</summary>
/// <param name="Spec">Account configuration specification.</param>
public sealed record CreateFinancialAccountCommand(FinancialAccountSpec Spec)
    : ICommand<FinancialAccountResponse>;
