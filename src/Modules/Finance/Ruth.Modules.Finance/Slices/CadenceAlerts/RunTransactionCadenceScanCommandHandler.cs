using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class RunTransactionCadenceScanCommandHandler(
    FinanceDbContext dbContext,
    IClock clock
) : ICommandHandler<RunTransactionCadenceScanCommand, CadenceScanSummaryResponse>
{
    public async Task<Result<CadenceScanSummaryResponse>> HandleAsync(
        RunTransactionCadenceScanCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var rules = await dbContext
            .CadenceAlertTypes.AsNoTracking()
            .Where(static r => r.IsActive)
            .ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return new CadenceScanSummaryResponse(0, 0);
        }

        var (byPerson, pastDueSet, personIds) = await LoadCandidateHistoriesAsync(
            cancellationToken
        );
        if (personIds.Length == 0)
        {
            return new CadenceScanSummaryResponse(0, 0);
        }

        var nowUtc = clock.UtcNow;
        var asOfDate = command.AsOfDate ?? DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var recentAlerts = await LoadRecentAlertKeysAsync(rules, nowUtc, cancellationToken);

        var createdCount = EvaluateAndStageAlerts(
            personIds,
            rules,
            byPerson,
            pastDueSet,
            recentAlerts,
            asOfDate,
            nowUtc
        );

        if (createdCount > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new CadenceScanSummaryResponse(personIds.Length, createdCount);
    }

    private async Task<(
        Dictionary<Guid, List<FinancialTransaction>> ByPerson,
        HashSet<Guid> PastDueSet,
        Guid[] PersonIds
    )> LoadCandidateHistoriesAsync(CancellationToken cancellationToken)
    {
        var txs = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Include(static t => t.Details)
            .Where(static t =>
                t.AuthorizedPersonId.HasValue
                && (
                    t.Status == FinancialTransactionStatus.Settled
                    || t.Status == FinancialTransactionStatus.PartiallyRefunded
                )
            )
            .OrderBy(static t => t.TransactionDateTimeUtc)
            .ToListAsync(cancellationToken);

        var pastDueList = await dbContext
            .RecurringSchedules.AsNoTracking()
            .Where(static s => s.Status == RecurringScheduleStatus.PastDue)
            .Select(static s => s.PersonId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pastDueSet = pastDueList.ToHashSet();

        var byPerson = txs.GroupBy(static t => t.AuthorizedPersonId!.Value)
            .ToDictionary(static g => g.Key, static g => g.ToList());

        var personIds = byPerson.Keys.Concat(pastDueSet).Distinct().ToArray();
        return (byPerson, pastDueSet, personIds);
    }

    private async Task<
        List<(Guid PersonId, TransactionCadenceAlertTypeId TypeId, DateTimeOffset DetectedOnUtc)>
    > LoadRecentAlertKeysAsync(
        List<TransactionCadenceAlertType> rules,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var cooldownFloor = nowUtc.AddDays(-rules.Max(static r => r.CooldownDays));
        var rows = await dbContext
            .CadenceAlerts.AsNoTracking()
            .Where(a => a.DetectedOnUtc >= cooldownFloor)
            .Select(static a => new
            {
                a.PersonId,
                a.AlertTypeId,
                a.DetectedOnUtc,
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(static r => (r.PersonId, r.AlertTypeId, r.DetectedOnUtc))];
    }

    private int EvaluateAndStageAlerts(
        Guid[] personIds,
        List<TransactionCadenceAlertType> rules,
        Dictionary<Guid, List<FinancialTransaction>> byPerson,
        HashSet<Guid> pastDueSet,
        List<(
            Guid PersonId,
            TransactionCadenceAlertTypeId TypeId,
            DateTimeOffset DetectedOnUtc
        )> recentAlerts,
        DateOnly asOfDate,
        DateTimeOffset nowUtc
    )
    {
        var createdCount = 0;
        foreach (var personId in personIds)
        {
            _ = byPerson.TryGetValue(personId, out var personTxs);
            var hasPastDue = pastDueSet.Contains(personId);

            foreach (var rule in rules)
            {
                var cutoff = nowUtc.AddDays(-rule.CooldownDays);
                if (
                    recentAlerts.Exists(a =>
                        a.PersonId == personId && a.TypeId == rule.Id && a.DetectedOnUtc >= cutoff
                    )
                )
                {
                    continue;
                }

                var finding = TransactionCadenceAnalyzer.Evaluate(
                    rule,
                    BuildSeriesForRule(personTxs, rule),
                    asOfDate,
                    hasPastDue
                );
                if (finding is { } detected)
                {
                    _ = dbContext.CadenceAlerts.Add(
                        TransactionCadenceAlert.Create(rule.Id, personId, detected, nowUtc)
                    );
                    createdCount++;
                }
            }
        }

        return createdCount;
    }

    private static HistoricalTransactionPoint[] BuildSeriesForRule(
        List<FinancialTransaction>? txs,
        TransactionCadenceAlertType rule
    )
    {
        if (txs is null || txs.Count == 0)
        {
            return [];
        }

        var points = new List<HistoricalTransactionPoint>(txs.Count);
        foreach (var tx in txs)
        {
            var amount = rule.AccountId is { } scopedAcc
                ? tx.Details.Where(d => d.AccountId == scopedAcc).Sum(static d => d.Amount)
                : tx.NetAmount;
            if (amount > 0m)
            {
                points.Add(
                    new HistoricalTransactionPoint(
                        DateOnly.FromDateTime(tx.TransactionDateTimeUtc.UtcDateTime),
                        amount
                    )
                );
            }
        }

        return [.. points];
    }
}
