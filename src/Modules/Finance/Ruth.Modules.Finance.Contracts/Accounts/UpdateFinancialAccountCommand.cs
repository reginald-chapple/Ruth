using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Command to update an existing financial account or fund.</summary>
/// <param name="AccountId">Target financial account identifier.</param>
/// <param name="Spec">Updated account configuration specification.</param>
public sealed record UpdateFinancialAccountCommand(Guid AccountId, FinancialAccountSpec Spec)
    : ICommand<FinancialAccountResponse>;
