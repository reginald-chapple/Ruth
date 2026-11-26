using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class EnrollCampaignParticipantCommandHandler(
    FinanceDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<EnrollCampaignParticipantCommand, CampaignParticipantResponse>
{
    public async Task<Result<CampaignParticipantResponse>> HandleAsync(
        EnrollCampaignParticipantCommand command,
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

        var displayName = await ResolveDisplayNameAsync(
            command.PersonId,
            command.DisplayName,
            cancellationToken
        );
        var enrolled = campaign.EnrollParticipant(
            command.PersonId,
            displayName,
            command.GroupMemberId,
            command.CustomGoalAmount,
            command.PersonalMessage,
            command.IsPublicProfileEnabled
        );
        if (enrolled.IsFailure)
        {
            return Result.Failure<CampaignParticipantResponse>(enrolled.Errors);
        }

        _ = dbContext.CampaignParticipants.Add(enrolled.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var rows = CampaignProgressCalculator.ProjectParticipants(
            campaign,
            new Dictionary<Guid, decimal>(),
            publicOnly: false
        );
        return rows.First(r => r.Id == enrolled.Value.Id.Value);
    }

    private async Task<string> ResolveDisplayNameAsync(
        Guid personId,
        string? explicitName,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return explicitName.Trim();
        }

        var personRes = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId),
            cancellationToken
        );
        return personRes.IsSuccess
            ? personRes.Value.FullName
            : $"Participant {personId.ToString("D")[..8]}";
    }
}
