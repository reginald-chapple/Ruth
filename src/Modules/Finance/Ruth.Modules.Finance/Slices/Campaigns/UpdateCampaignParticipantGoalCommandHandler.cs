using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class UpdateCampaignParticipantGoalCommandHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader
) : ICommandHandler<UpdateCampaignParticipantGoalCommand, CampaignParticipantResponse>
{
    public async Task<Result<CampaignParticipantResponse>> HandleAsync(
        UpdateCampaignParticipantGoalCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var campaignId = FundraisingCampaignId.From(command.CampaignId);
        var campaign = await dbContext
            .FundraisingCampaigns.Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var participantId = CampaignParticipantId.From(command.ParticipantId);
        var updated = campaign.UpdateParticipantGoal(
            participantId,
            command.CustomGoalAmount,
            command.PersonalMessage,
            command.IsFundraisingEnabled,
            command.IsPublicProfileEnabled
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CampaignParticipantResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        var rows = CampaignProgressCalculator.ProjectParticipants(
            campaign,
            netMap,
            publicOnly: false
        );
        return rows.First(r => r.Id == updated.Value.Id.Value);
    }
}
