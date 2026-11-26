using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal sealed class GetFinancialAccountByIdQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetFinancialAccountByIdQuery, FinancialAccountResponse>
{
    public async Task<Result<FinancialAccountResponse>> HandleAsync(
        GetFinancialAccountByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountId = FinancialAccountId.From(query.AccountId);
        var account = await dbContext
            .FinancialAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        return account is null
            ? FinanceErrors.AccountNotFound
            : FinancialAccountMapper.ToResponse(account);
    }
}
