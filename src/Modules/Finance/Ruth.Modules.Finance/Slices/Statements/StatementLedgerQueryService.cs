using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class StatementLedgerQueryService(FinanceDbContext dbContext)
{
    public async Task<AggregatedStatementLedger> BuildStatementLedgerAsync(
        FinancialStatementTemplate template,
        IReadOnlyCollection<Guid> memberPersonIds,
        Guid? householdId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken
    )
    {
        var memberArray = memberPersonIds.ToArray();
        var transactions = await LoadPeriodTransactionsAsync(
            memberArray,
            householdId,
            periodStartDate,
            periodEndDate,
            cancellationToken
        );
        var invoices = await dbContext
            .Invoices.AsNoTracking()
            .Where(i =>
                i.IssueDate <= periodEndDate
                && (
                    memberArray.Contains(i.PersonId)
                    || (householdId.HasValue && i.HouseholdId == householdId)
                )
                && (
                    i.IssueDate >= periodStartDate
                    || i.Status == InvoiceStatus.Open
                    || i.Status == InvoiceStatus.PartiallyPaid
                )
            )
            .ToListAsync(cancellationToken);

        var accounts = await dbContext
            .FinancialAccounts.AsNoTracking()
            .ToDictionaryAsync(
                static a => a.Id,
                static a => (a.PublicName, a.Code),
                cancellationToken
            );

        var pledges = await LoadPledgeSummariesAsync(
            memberArray,
            template.CurrencyCode,
            periodStartDate,
            periodEndDate,
            accounts,
            transactions,
            cancellationToken
        );

        return StatementLedgerAggregator.Aggregate(
            template.StatementKind,
            template.CurrencyCode,
            template.IncludeOpenInvoices,
            template.IncludePledgeSummary,
            transactions,
            accounts,
            invoices,
            pledges
        );
    }

    private async Task<List<FinancialTransaction>> LoadPeriodTransactionsAsync(
        Guid[] memberArray,
        Guid? householdId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken
    )
    {
        var startUtc = new DateTimeOffset(
            periodStartDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );
        var endExclusiveUtc = new DateTimeOffset(
            periodEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );

        return await dbContext
            .FinancialTransactions.AsNoTracking()
            .Include(static t => t.Details)
            .Where(t =>
                t.TransactionDateTimeUtc >= startUtc
                && t.TransactionDateTimeUtc < endExclusiveUtc
                && (
                    (
                        t.AuthorizedPersonId.HasValue
                        && memberArray.Contains(t.AuthorizedPersonId.Value)
                    ) || (householdId.HasValue && t.HouseholdId == householdId)
                )
            )
            .ToListAsync(cancellationToken);
    }

    private async Task<List<StatementPledgeSummaryDto>> LoadPledgeSummariesAsync(
        Guid[] memberIds,
        string currencyCode,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        Dictionary<FinancialAccountId, (string Name, string Code)> accounts,
        List<FinancialTransaction> periodTransactions,
        CancellationToken cancellationToken
    )
    {
        if (memberIds.Length == 0)
        {
            return [];
        }

        var pledges = await dbContext
            .FinancialPledges.AsNoTracking()
            .Where(p =>
                memberIds.Contains(p.PersonId)
                && p.Status == FinancialPledgeStatus.Active
                && p.StartDate <= periodEndDate
                && p.EndDate >= periodStartDate
            )
            .ToListAsync(cancellationToken);

        return
        [
            .. pledges
                .Where(p =>
                    string.Equals(p.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)
                )
                .Select(p => BuildPledgeSummary(p, accounts, periodTransactions)),
        ];
    }

    private static StatementPledgeSummaryDto BuildPledgeSummary(
        FinancialPledge pledge,
        Dictionary<FinancialAccountId, (string Name, string Code)> accounts,
        List<FinancialTransaction> periodTransactions
    )
    {
        var accountName = accounts.TryGetValue(pledge.AccountId, out var meta)
            ? meta.Name
            : "Charitable Fund";
        var fulfilled = periodTransactions
            .Where(static t => t.TotalAmount > 0m && t.NetAmount > 0m)
            .SelectMany(t =>
                t.Details.Where(d => d.AccountId == pledge.AccountId)
                    .Select(d =>
                        decimal.Round(
                            d.Amount * (t.NetAmount / t.TotalAmount),
                            2,
                            MidpointRounding.AwayFromZero
                        )
                    )
            )
            .Sum();
        var remaining = Math.Max(0m, pledge.TotalAmount - fulfilled);
        return new StatementPledgeSummaryDto(
            pledge.Id.Value,
            accountName,
            pledge.StartDate,
            pledge.EndDate,
            pledge.TotalAmount,
            fulfilled,
            remaining,
            pledge.CurrencyCode
        );
    }
}
