using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class GetPledgeAnalyticsQueryHandler(
    FinanceDbContext dbContext,
    IFinanceLedgerSummaryReader ledgerSummaryReader,
    IClock clock
) : IQueryHandler<GetPledgeAnalyticsQuery, PledgeAnalyticsSummaryResponse>
{
    public async Task<Result<PledgeAnalyticsSummaryResponse>> HandleAsync(
        GetPledgeAnalyticsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!IsoCurrencyCatalog.TryGetScale(query.CurrencyCode, out var currency, out _))
        {
            return FinanceErrors.CurrencyCodeInvalid;
        }

        var pledges = await LoadMatchingPledgesAsync(query, currency, cancellationToken);
        if (pledges.Count == 0)
        {
            return new PledgeAnalyticsSummaryResponse(currency, 0, 0, 0m, 0m, 0m, 0m, 0, 0);
        }

        var settledMap = await ledgerSummaryReader.GetSettledContributionsAsync(
            [.. pledges.Select(static p => p.PersonId).Distinct()],
            [.. pledges.Select(static p => p.AccountId.Value).Distinct()],
            pledges.Min(static p => p.StartDate),
            pledges.Max(static p => p.EndDate),
            cancellationToken
        );

        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return Summarize(pledges, settledMap, currency, asOfDate);
    }

    private async Task<List<FinancialPledge>> LoadMatchingPledgesAsync(
        GetPledgeAnalyticsQuery query,
        string currency,
        CancellationToken cancellationToken
    )
    {
        var pledgesQuery = dbContext
            .FinancialPledges.AsNoTracking()
            .Where(p => p.CurrencyCode == currency && p.Status != FinancialPledgeStatus.Cancelled);

        if (query.AccountId is { } accountGuid && accountGuid != Guid.Empty)
        {
            var accountId = FinancialAccountId.From(accountGuid);
            pledgesQuery = pledgesQuery.Where(p => p.AccountId == accountId);
        }

        if (query.BranchId is { } branchId && branchId != Guid.Empty)
        {
            pledgesQuery = pledgesQuery.Where(p => p.BranchId == branchId);
        }

        return await pledgesQuery.ToListAsync(cancellationToken);
    }

    private static PledgeAnalyticsSummaryResponse Summarize(
        List<FinancialPledge> pledges,
        IReadOnlyDictionary<(Guid PersonId, Guid AccountId), decimal> settledMap,
        string currency,
        DateOnly asOfDate
    )
    {
        var activeCount = 0;
        var completedCount = 0;
        var totalPledged = 0m;
        var totalFulfilled = 0m;
        var totalRemaining = 0m;
        var onTrack = 0;
        var behind = 0;

        foreach (var pledge in pledges)
        {
            _ = settledMap.TryGetValue(
                (pledge.PersonId, pledge.AccountId.Value),
                out var fulfilled
            );
            totalPledged += pledge.TotalAmount;
            totalFulfilled += fulfilled;

            if (pledge.Status == FinancialPledgeStatus.Completed || fulfilled >= pledge.TotalAmount)
            {
                completedCount++;
                continue;
            }

            activeCount++;
            totalRemaining += Math.Max(0m, pledge.TotalAmount - fulfilled);
            if (fulfilled >= pledge.CalculateExpectedByDate(asOfDate))
            {
                onTrack++;
            }
            else
            {
                behind++;
            }
        }

        var pct =
            totalPledged > 0m
                ? decimal.Round(
                    totalFulfilled / totalPledged * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : 0m;

        return new PledgeAnalyticsSummaryResponse(
            currency,
            activeCount,
            completedCount,
            totalPledged,
            totalFulfilled,
            totalRemaining,
            pct,
            onTrack,
            behind
        );
    }
}
