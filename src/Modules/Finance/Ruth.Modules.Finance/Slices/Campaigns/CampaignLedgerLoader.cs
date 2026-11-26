using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class CampaignLedgerLoader(FinanceDbContext dbContext)
{
    public async Task<Dictionary<Guid, decimal>> LoadNetTransactionAmountsAsync(
        IEnumerable<CampaignContributionCredit> credits,
        CancellationToken cancellationToken
    )
    {
        var txIds = credits.Select(static c => c.TransactionId).Distinct().ToArray();
        return txIds.Length == 0
            ? []
            : await dbContext
                .FinancialTransactions.AsNoTracking()
                .Where(t => txIds.Contains(t.Id))
                .ToDictionaryAsync(
                    static t => t.Id.Value,
                    static t => t.NetAmount,
                    cancellationToken
                );
    }
}
