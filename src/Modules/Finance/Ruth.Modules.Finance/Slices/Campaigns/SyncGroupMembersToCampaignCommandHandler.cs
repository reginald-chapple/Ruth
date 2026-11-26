using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class SyncGroupMembersToCampaignCommandHandler(
    FinanceDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    CampaignLedgerLoader ledgerLoader
) : ICommandHandler<SyncGroupMembersToCampaignCommand, IReadOnlyList<CampaignParticipantResponse>>
{
    public async Task<Result<IReadOnlyList<CampaignParticipantResponse>>> HandleAsync(
        SyncGroupMembersToCampaignCommand command,
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

        var membersRes = await queryDispatcher.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(campaign.GroupId, GroupMemberStatus.Active), cancellationToken);
        if (membersRes.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CampaignParticipantResponse>>(membersRes.Errors);
        }

        var addedAny = false;
        foreach (var member in membersRes.Value)
        {
            if (campaign.Participants.Any(p => p.PersonId == member.PersonId))
            {
                continue;
            }

            var displayName = await ResolvePersonNameAsync(member.PersonId, cancellationToken);
            var enrolled = campaign.EnrollParticipant(member.PersonId, displayName, member.Id);
            if (enrolled.IsSuccess)
            {
                _ = dbContext.CampaignParticipants.Add(enrolled.Value);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        return Result.Success(
            CampaignProgressCalculator.ProjectParticipants(campaign, netMap, publicOnly: false)
        );
    }

    private async Task<string> ResolvePersonNameAsync(
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var personRes = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId),
            cancellationToken
        );
        return personRes.IsSuccess
            ? personRes.Value.FullName
            : $"Member {personId.ToString("D")[..8]}";
    }
}
