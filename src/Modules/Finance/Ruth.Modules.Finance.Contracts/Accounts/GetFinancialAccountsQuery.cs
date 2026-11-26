using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Query to list financial accounts with optional filtering by kind, branch, tax deductibility, or active status.</summary>
/// <param name="AccountKind">Optional filter by account kind.</param>
/// <param name="BranchId">Optional filter by Branch/Site identifier.</param>
/// <param name="IsTaxDeductible">Optional filter by tax deductibility.</param>
/// <param name="ActiveOnly">When true, returns only active accounts.</param>
/// <param name="PublicOnly">When true, returns only publicly visible accounts.</param>
public sealed record GetFinancialAccountsQuery(
    FinancialAccountKind? AccountKind = null,
    Guid? BranchId = null,
    bool? IsTaxDeductible = null,
    bool ActiveOnly = false,
    bool PublicOnly = false
) : IQuery<IReadOnlyList<FinancialAccountResponse>>;
