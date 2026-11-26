using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Query to fetch a single financial account by identifier.</summary>
/// <param name="AccountId">Target financial account identifier.</param>
public sealed record GetFinancialAccountByIdQuery(Guid AccountId)
    : IQuery<FinancialAccountResponse>;
