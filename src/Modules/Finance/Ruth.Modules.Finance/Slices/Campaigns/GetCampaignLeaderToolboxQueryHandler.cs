using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class GetCampaignLeaderToolboxQueryHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader
) : IQueryHandler<GetCampaignLeaderToolboxQuery, CampaignLeaderToolboxResponse>
{
    public async Task<Result<CampaignLeaderToolboxResponse>> HandleAsync(
        GetCampaignLeaderToolboxQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = FundraisingCampaignId.From(query.CampaignId);
        var campaign = await dbContext
            .FundraisingCampaigns.AsNoTracking()
            .Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        return new CampaignLeaderToolboxResponse(
            CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap),
            CampaignProgressCalculator.ProjectParticipants(campaign, netMap, publicOnly: false),
            CampaignProgressCalculator.ProjectLeaderContributions(campaign, netMap)
        );
    }
}
