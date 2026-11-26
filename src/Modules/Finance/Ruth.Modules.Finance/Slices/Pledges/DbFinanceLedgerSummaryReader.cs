using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Infrastructure;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class DbFinanceLedgerSummaryReader(FinanceDbContext dbContext)
    : IFinanceLedgerSummaryReader
{
    public async Task<
        IReadOnlyDictionary<(Guid PersonId, Guid AccountId), decimal>
    > GetSettledContributionsAsync(
        IReadOnlyCollection<Guid> personIds,
        IReadOnlyCollection<Guid> accountIds,
        DateOnly minDate,
        DateOnly maxDate,
        CancellationToken cancellationToken = default
    )
    {
        if (personIds.Count == 0 || accountIds.Count == 0)
        {
            return new Dictionary<(Guid PersonId, Guid AccountId), decimal>();
        }

        var fromUtc = new DateTimeOffset(minDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(maxDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var typedAccountIds = accountIds.Select(FinancialAccountId.From).ToArray();

        var txs = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Include(static t => t.Details)
            .Where(t =>
                t.AuthorizedPersonId.HasValue
                && personIds.Contains(t.AuthorizedPersonId.Value)
                && t.TransactionDateTimeUtc >= fromUtc
                && t.TransactionDateTimeUtc <= toUtc
                && (
                    t.Status == FinancialTransactionStatus.Settled
                    || t.Status == FinancialTransactionStatus.PartiallyRefunded
                )
            )
            .ToListAsync(cancellationToken);

        var result = new Dictionary<(Guid PersonId, Guid AccountId), decimal>();
        foreach (var tx in txs)
        {
            if (tx.TotalAmount <= 0m || tx.AuthorizedPersonId is null)
            {
                continue;
            }

            var retainedRatio = tx.NetAmount / tx.TotalAmount;
            foreach (var detail in tx.Details)
            {
                if (!typedAccountIds.Contains(detail.AccountId))
                {
                    continue;
                }

                var key = (tx.AuthorizedPersonId.Value, detail.AccountId.Value);
                _ = result.TryGetValue(key, out var existing);
                result[key] =
                    existing
                    + decimal.Round(
                        detail.Amount * retainedRatio,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            }
        }

        return result;
    }

    public async Task<(
        decimal OpenInvoiceBalance,
        decimal LifetimeSettledTotal
    )> GetConstituentTotalsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var openInvoices = await dbContext
            .Invoices.AsNoTracking()
            .Where(i =>
                i.PersonId == personId
                && (i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid)
            )
            .ToListAsync(cancellationToken);
        var openBalance = openInvoices.Sum(static i => i.BalanceDue);

        var settledTxs = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Where(t =>
                t.AuthorizedPersonId == personId
                && (
                    t.Status == FinancialTransactionStatus.Settled
                    || t.Status == FinancialTransactionStatus.PartiallyRefunded
                )
            )
            .ToListAsync(cancellationToken);
        var settledTotal = settledTxs.Sum(static t => t.NetAmount);

        return (openBalance, settledTotal);
    }
}
