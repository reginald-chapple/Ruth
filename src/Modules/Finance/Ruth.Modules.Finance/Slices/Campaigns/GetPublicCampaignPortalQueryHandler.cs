using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class GetPublicCampaignPortalQueryHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader,
    IClock clock
) : IQueryHandler<GetPublicCampaignPortalQuery, PublicCampaignPortalResponse>
{
    public async Task<Result<PublicCampaignPortalResponse>> HandleAsync(
        GetPublicCampaignPortalQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query.Slug))
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var slug = query.Slug.Trim().ToLowerInvariant();
        var campaign = await dbContext
            .FundraisingCampaigns.AsNoTracking()
            .Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
        if (campaign is null || !campaign.AllowPublicContributions)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        var summary = CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        return new PublicCampaignPortalResponse(
            campaign.Id.Value,
            campaign.Name,
            campaign.Slug,
            campaign.Summary,
            campaign.PublicDescription,
            campaign.CurrencyCode,
            summary.EffectiveTeamGoalAmount,
            summary.TotalRaisedAmount,
            summary.ProgressPercent,
            summary.DonorCount,
            campaign.IsOpenForPublicContributions(today),
            campaign.DefaultFinancialGatewayId?.Value,
            CampaignProgressCalculator.ProjectParticipants(campaign, netMap, publicOnly: true),
            CampaignProgressCalculator.ProjectPublicDonorFeed(campaign, netMap)
        );
    }
}
