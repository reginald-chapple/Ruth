using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class GetFundraisingCampaignsQueryHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader
) : IQueryHandler<GetFundraisingCampaignsQuery, IReadOnlyList<FundraisingCampaignResponse>>
{
    public async Task<Result<IReadOnlyList<FundraisingCampaignResponse>>> HandleAsync(
        GetFundraisingCampaignsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var efQuery = dbContext
            .FundraisingCampaigns.AsNoTracking()
            .Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .AsQueryable();

        if (query.GroupId.HasValue && query.GroupId.Value != Guid.Empty)
        {
            var gId = query.GroupId.Value;
            efQuery = efQuery.Where(c => c.GroupId == gId);
        }

        if (query.FinancialAccountId.HasValue && query.FinancialAccountId.Value != Guid.Empty)
        {
            var accId = FinancialAccountId.From(query.FinancialAccountId.Value);
            efQuery = efQuery.Where(c => c.FinancialAccountId == accId);
        }

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            efQuery = efQuery.Where(c => c.Status == status);
        }

        var campaigns = await efQuery
            .OrderByDescending(static c => c.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaigns.SelectMany(static c => c.Credits),
            cancellationToken
        );

        return campaigns
            .Select(c => CampaignProgressCalculator.ProjectCampaignSummary(c, netMap))
            .ToArray();
    }
}
