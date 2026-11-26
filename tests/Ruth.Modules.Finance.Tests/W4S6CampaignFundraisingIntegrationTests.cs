using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S6CampaignFundraisingIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task CampaignFundraising_GroupSync_GoalOverrides_PublicPortalPrivacy_AndLeaderToolbox_WorkEndToEnd()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var accounts = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery(ActiveOnly: true))
        ).Value;
        var fundAccountId = accounts.First(static a => a.IsTaxDeductible).Id;

        var gateways = (
            await queries.QueryAsync<
                GetFinancialGatewaysQuery,
                IReadOnlyList<FinancialGatewayResponse>
            >(new GetFinancialGatewaysQuery(ActiveOnly: true))
        ).Value;
        var gatewayId = gateways[0].Id;

        var (groupId, _, _) = await W4S6GroupSeedTestHelper.CreateFundraisingTeamAsync(commands);

        var campaignId = await SetupCampaignParticipantsAndOverridesAsync(
            commands,
            groupId,
            fundAccountId,
            gatewayId
        );

        await VerifyContributionsPrivacyAndLeaderToolboxAsync(commands, queries, campaignId);
    }

    private static async Task<Guid> SetupCampaignParticipantsAndOverridesAsync(
        ICommandDispatcher commands,
        Guid groupId,
        Guid fundAccountId,
        Guid gatewayId
    )
    {
        var campaign = (
            await commands.SendAsync<CreateFundraisingCampaignCommand, FundraisingCampaignResponse>(
                new CreateFundraisingCampaignCommand(
                    new FundraisingCampaignSpec(
                        "Clean Water Expedition 2026",
                        "clean-water-expedition-2026",
                        groupId,
                        fundAccountId,
                        CampaignGoalAmount: 2000.00m,
                        DefaultParticipantGoalAmount: 800.00m,
                        DefaultFinancialGatewayId: gatewayId
                    )
                )
            )
        ).Value;

        var overridden = (
            await commands.SendAsync<
                SetCampaignTeamGoalOverrideCommand,
                FundraisingCampaignResponse
            >(new SetCampaignTeamGoalOverrideCommand(campaign.Id, 2500.00m))
        ).Value;
        Assert.Equal(2500.00m, overridden.EffectiveTeamGoalAmount);

        var synced = (
            await commands.SendAsync<
                SyncGroupMembersToCampaignCommand,
                IReadOnlyList<CampaignParticipantResponse>
            >(new SyncGroupMembersToCampaignCommand(campaign.Id))
        ).Value;
        Assert.Equal(2, synced.Count);

        var updatedPart = (
            await commands.SendAsync<
                UpdateCampaignParticipantGoalCommand,
                CampaignParticipantResponse
            >(
                new UpdateCampaignParticipantGoalCommand(
                    campaign.Id,
                    synced[0].Id,
                    CustomGoalAmount: 1200.00m,
                    PersonalMessage: "Helping build two community wells!"
                )
            )
        ).Value;
        Assert.Equal(1200.00m, updatedPart.EffectiveGoalAmount);
        return campaign.Id;
    }

    private static async Task VerifyContributionsPrivacyAndLeaderToolboxAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid campaignId
    )
    {
        var portalBefore = (
            await queries.QueryAsync<GetPublicCampaignPortalQuery, PublicCampaignPortalResponse>(
                new GetPublicCampaignPortalQuery("clean-water-expedition-2026")
            )
        ).Value;
        var targetParticipantId = portalBefore.PublicParticipants[0].Id;

        var firstGive = (
            await commands.SendAsync<
                SubmitCampaignContributionCommand,
                CampaignContributionReceiptResponse
            >(
                new SubmitCampaignContributionCommand(
                    "clean-water-expedition-2026",
                    600.00m,
                    "USD",
                    "Casey Supporters",
                    CampaignDonorPrivacy.PublicNameHideAmount,
                    CreditedParticipantId: targetParticipantId,
                    OneTimeHostedToken: "tok_portal_visa_1",
                    TenderType: FinancialTenderType.CreditCard,
                    DonorComment: "Proud of this team!"
                )
            )
        ).Value;
        Assert.Equal(600.00m, firstGive.UpdatedCampaignTotalRaised);

        _ = await commands.SendAsync<
            SubmitCampaignContributionCommand,
            CampaignContributionReceiptResponse
        >(
            new SubmitCampaignContributionCommand(
                "clean-water-expedition-2026",
                400.00m,
                "USD",
                "Hidden Staff Donor",
                CampaignDonorPrivacy.PrivateStaffOnly,
                OneTimeHostedToken: "tok_portal_visa_2",
                TenderType: FinancialTenderType.CreditCard
            )
        );

        var portalAfter = (
            await queries.QueryAsync<GetPublicCampaignPortalQuery, PublicCampaignPortalResponse>(
                new GetPublicCampaignPortalQuery("clean-water-expedition-2026")
            )
        ).Value;
        Assert.Equal(1000.00m, portalAfter.TotalRaisedAmount);
        Assert.Equal(40.00m, portalAfter.ProgressPercent);
        _ = Assert.Single(portalAfter.RecentPublicContributions);
        Assert.Null(portalAfter.RecentPublicContributions[0].VisibleAmount);

        var toolbox = (
            await queries.QueryAsync<GetCampaignLeaderToolboxQuery, CampaignLeaderToolboxResponse>(
                new GetCampaignLeaderToolboxQuery(campaignId)
            )
        ).Value;
        Assert.Equal(2, toolbox.Contributions.Count);
        Assert.Equal(600.00m, toolbox.Participants[0].RaisedAmount);
    }
}
