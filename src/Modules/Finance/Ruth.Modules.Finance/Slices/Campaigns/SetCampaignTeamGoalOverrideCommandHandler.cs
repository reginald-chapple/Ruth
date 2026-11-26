using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class SetCampaignTeamGoalOverrideCommandHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader
) : ICommandHandler<SetCampaignTeamGoalOverrideCommand, FundraisingCampaignResponse>
{
    public async Task<Result<FundraisingCampaignResponse>> HandleAsync(
        SetCampaignTeamGoalOverrideCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = FundraisingCampaignId.From(command.CampaignId);
        var campaign = await dbContext
            .FundraisingCampaigns.Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var updated = campaign.SetTeamGoalOverride(command.TeamGoalOverrideAmount);
        if (updated.IsFailure)
        {
            return Result.Failure<FundraisingCampaignResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        return CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap);
    }
}
