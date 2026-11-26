using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class GetFinancialTransactionsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetFinancialTransactionsQuery, IReadOnlyList<FinancialTransactionResponse>>
{
    public async Task<Result<IReadOnlyList<FinancialTransactionResponse>>> HandleAsync(
        GetFinancialTransactionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var clamped = Math.Clamp(query.MaxResults, 1, 500);
        var baseQuery = dbContext
            .FinancialTransactions.AsNoTracking()
            .Include(static t => t.Details)
            .Include(static t => t.Images)
            .Include(static t => t.Refunds);

        var txs = await ApplyFilters(baseQuery, query)
            .OrderByDescending(static t => t.TransactionDateTimeUtc)
            .Take(clamped)
            .ToListAsync(cancellationToken);

        if (txs.Count == 0)
        {
            IReadOnlyList<FinancialTransactionResponse> empty = [];
            return Result.Success(empty);
        }

        var accountIds = txs.SelectMany(static t => t.Details.Select(static d => d.AccountId))
            .Distinct()
            .ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        IReadOnlyList<FinancialTransactionResponse> mapped =
        [
            .. txs.Select(t => FinancialTransactionMapper.ToResponse(t, names)),
        ];
        return Result.Success(mapped);
    }

    private static IQueryable<FinancialTransaction> ApplyFilters(
        IQueryable<FinancialTransaction> source,
        GetFinancialTransactionsQuery query
    )
    {
        if (query.BatchId is { } batchGuid && batchGuid != Guid.Empty)
        {
            var batchId = FinancialBatchId.From(batchGuid);
            source = source.Where(t => t.BatchId == batchId);
        }

        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            source = source.Where(t => t.AuthorizedPersonId == personId);
        }

        if (query.AccountId is { } accountGuid && accountGuid != Guid.Empty)
        {
            var accountId = FinancialAccountId.From(accountGuid);
            source = source.Where(t => t.Details.Any(d => d.AccountId == accountId));
        }

        if (query.FromUtc is { } fromUtc)
        {
            source = source.Where(t => t.TransactionDateTimeUtc >= fromUtc);
        }

        if (query.ToUtc is { } toUtc)
        {
            source = source.Where(t => t.TransactionDateTimeUtc <= toUtc);
        }

        return source;
    }
}
