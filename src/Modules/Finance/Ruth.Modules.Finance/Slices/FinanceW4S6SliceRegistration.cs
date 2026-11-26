using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Slices.Campaigns;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S6SliceRegistration
{
    public static void RegisterW4S6Services(IServiceCollection services)
    {
        services.TryAddScoped<CampaignLedgerLoader>();

        _ = services.AddCommandHandler<
            CreateFundraisingCampaignCommand,
            FundraisingCampaignResponse,
            CreateFundraisingCampaignCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFundraisingCampaignCommand,
            FundraisingCampaignResponse,
            UpdateFundraisingCampaignCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetCampaignTeamGoalOverrideCommand,
            FundraisingCampaignResponse,
            SetCampaignTeamGoalOverrideCommandHandler
        >();
        _ = services.AddCommandHandler<
            EnrollCampaignParticipantCommand,
            CampaignParticipantResponse,
            EnrollCampaignParticipantCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCampaignParticipantGoalCommand,
            CampaignParticipantResponse,
            UpdateCampaignParticipantGoalCommandHandler
        >();
        _ = services.AddCommandHandler<
            SyncGroupMembersToCampaignCommand,
            IReadOnlyList<CampaignParticipantResponse>,
            SyncGroupMembersToCampaignCommandHandler
        >();
        _ = services.AddCommandHandler<
            SubmitCampaignContributionCommand,
            CampaignContributionReceiptResponse,
            SubmitCampaignContributionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFundraisingCampaignsQuery,
            IReadOnlyList<FundraisingCampaignResponse>,
            GetFundraisingCampaignsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCampaignLeaderToolboxQuery,
            CampaignLeaderToolboxResponse,
            GetCampaignLeaderToolboxQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPublicCampaignPortalQuery,
            PublicCampaignPortalResponse,
            GetPublicCampaignPortalQueryHandler
        >();
    }
}
