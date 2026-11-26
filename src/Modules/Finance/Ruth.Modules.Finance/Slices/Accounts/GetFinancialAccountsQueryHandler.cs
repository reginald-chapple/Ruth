using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal sealed class GetFinancialAccountsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetFinancialAccountsQuery, IReadOnlyList<FinancialAccountResponse>>
{
    public async Task<Result<IReadOnlyList<FinancialAccountResponse>>> HandleAsync(
        GetFinancialAccountsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountsQuery = dbContext.FinancialAccounts.AsNoTracking().AsQueryable();

        if (query.AccountKind is { } kind)
        {
            accountsQuery = accountsQuery.Where(a => a.AccountKind == kind);
        }

        if (query.BranchId is { } branchId && branchId != Guid.Empty)
        {
            accountsQuery = accountsQuery.Where(a => a.BranchId == branchId);
        }

        if (query.IsTaxDeductible is { } taxDeductible)
        {
            accountsQuery = accountsQuery.Where(a => a.IsTaxDeductible == taxDeductible);
        }

        if (query.ActiveOnly)
        {
            accountsQuery = accountsQuery.Where(static a => a.IsActive);
        }

        if (query.PublicOnly)
        {
            accountsQuery = accountsQuery.Where(static a => a.IsPublic);
        }

        var rows = await accountsQuery
            .OrderBy(static a => a.DisplayOrder)
            .ThenBy(static a => a.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<FinancialAccountResponse> mapped =
        [
            .. rows.Select(FinancialAccountMapper.ToResponse),
        ];
        return Result.Success(mapped);
    }
}
