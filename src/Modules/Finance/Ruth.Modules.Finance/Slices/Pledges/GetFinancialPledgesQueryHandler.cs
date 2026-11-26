using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class GetFinancialPledgesQueryHandler(
    FinanceDbContext dbContext,
    IFinanceLedgerSummaryReader ledgerSummaryReader,
    IClock clock
) : IQueryHandler<GetFinancialPledgesQuery, IReadOnlyList<FinancialPledgeResponse>>
{
    public async Task<Result<IReadOnlyList<FinancialPledgeResponse>>> HandleAsync(
        GetFinancialPledgesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var pledges = await ApplyFilters(dbContext.FinancialPledges.AsNoTracking(), query)
            .OrderByDescending(static p => p.StartDate)
            .ToListAsync(cancellationToken);

        if (pledges.Count == 0)
        {
            IReadOnlyList<FinancialPledgeResponse> empty = [];
            return Result.Success(empty);
        }

        var accountIds = pledges.Select(static p => p.AccountId).Distinct().ToArray();
        var accountNames = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        var settledMap = await ledgerSummaryReader.GetSettledContributionsAsync(
            [.. pledges.Select(static p => p.PersonId).Distinct()],
            [.. accountIds.Select(static id => id.Value)],
            pledges.Min(static p => p.StartDate),
            pledges.Max(static p => p.EndDate),
            cancellationToken
        );

        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        IReadOnlyList<FinancialPledgeResponse> mapped =
        [
            .. pledges.Select(p =>
            {
                _ = settledMap.TryGetValue((p.PersonId, p.AccountId.Value), out var fulfilled);
                var name = accountNames.GetValueOrDefault(p.AccountId, "Account");
                return FinancialPledgeMapper.ToResponse(p, name, fulfilled, asOfDate);
            }),
        ];
        return Result.Success(mapped);
    }

    private static IQueryable<FinancialPledge> ApplyFilters(
        IQueryable<FinancialPledge> source,
        GetFinancialPledgesQuery query
    )
    {
        if (query.AccountId is { } accountGuid && accountGuid != Guid.Empty)
        {
            var accountId = FinancialAccountId.From(accountGuid);
            source = source.Where(p => p.AccountId == accountId);
        }

        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            source = source.Where(p => p.PersonId == personId);
        }

        if (query.Status is { } status)
        {
            source = source.Where(p => p.Status == status);
        }

        return source;
    }
}
