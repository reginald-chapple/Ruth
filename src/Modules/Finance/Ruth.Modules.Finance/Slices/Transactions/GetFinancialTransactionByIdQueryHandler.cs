using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class GetFinancialTransactionByIdQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetFinancialTransactionByIdQuery, FinancialTransactionResponse>
{
    public async Task<Result<FinancialTransactionResponse>> HandleAsync(
        GetFinancialTransactionByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var txId = FinancialTransactionId.From(query.TransactionId);
        var tx = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Include(static t => t.Details)
            .Include(static t => t.Images)
            .Include(static t => t.Refunds)
            .FirstOrDefaultAsync(t => t.Id == txId, cancellationToken);
        if (tx is null)
        {
            return FinanceErrors.TransactionNotFound;
        }

        var accountIds = tx.Details.Select(static d => d.AccountId).Distinct().ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        return FinancialTransactionMapper.ToResponse(tx, names);
    }
}
