using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Domain.Transactions;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S6CampaignFundraisingDomainTests
{
    [Fact]
    public void Campaign_EnforcesTeamAndParticipantGoalOverrides_AndPrivacyProjections()
    {
        var accountId = FinancialAccountId.Create().Value;
        var groupId = Guid.CreateVersion7();

        var lockedCampaign = FundraisingCampaign
            .Create(
                new FundraisingCampaignSpec(
                    "Locked Goals Project",
                    "locked-goals",
                    groupId,
                    accountId,
                    CampaignGoalAmount: 5000.00m,
                    DefaultParticipantGoalAmount: 500.00m,
                    AllowTeamGoalOverride: false,
                    AllowParticipantGoalOverride: false
                )
            )
            .Value;

        Assert.True(lockedCampaign.SetTeamGoalOverride(6000.00m).IsFailure);
        Assert.True(
            lockedCampaign
                .EnrollParticipant(Guid.CreateVersion7(), "Sam Carter", customGoalAmount: 750.00m)
                .IsFailure
        );

        var campaign = FundraisingCampaign
            .Create(
                new FundraisingCampaignSpec(
                    "Community Garden Build",
                    "garden-build-2026",
                    groupId,
                    accountId,
                    CampaignGoalAmount: 2000.00m,
                    DefaultParticipantGoalAmount: 500.00m,
                    AllowTeamGoalOverride: true,
                    AllowParticipantGoalOverride: true
                )
            )
            .Value;

        Assert.True(campaign.SetTeamGoalOverride(2500.00m).IsSuccess);
        Assert.Equal(2500.00m, campaign.EffectiveTeamGoalAmount);

        var p1 = campaign
            .EnrollParticipant(Guid.CreateVersion7(), "Jordan Lee", customGoalAmount: 1000.00m)
            .Value;
        var p2 = campaign.EnrollParticipant(Guid.CreateVersion7(), "Riley Chen").Value;
        Assert.Equal(1000.00m, p1.GetEffectiveGoal(campaign.DefaultParticipantGoalAmount));
        Assert.Equal(500.00m, p2.GetEffectiveGoal(campaign.DefaultParticipantGoalAmount));

        VerifyPrivacyAndRefundNetAdjustments(campaign, p1, p2);
    }

    private static void VerifyPrivacyAndRefundNetAdjustments(
        FundraisingCampaign campaign,
        CampaignParticipant p1,
        CampaignParticipant p2
    )
    {
        var tx1 = FinancialTransactionId.Create();
        var tx2 = FinancialTransactionId.Create();
        var tx3 = FinancialTransactionId.Create();
        var tx4 = FinancialTransactionId.Create();
        var now = DateTimeOffset.UtcNow;

        _ = campaign.RecordContributionCredit(
            tx1,
            400.00m,
            "Donor One",
            CampaignDonorPrivacy.PublicNameAndAmount,
            now,
            creditedParticipantId: p1.Id,
            donorComment: "Go Jordan!"
        );
        _ = campaign.RecordContributionCredit(
            tx2,
            300.00m,
            "Donor Two",
            CampaignDonorPrivacy.PublicNameHideAmount,
            now.AddMinutes(1),
            creditedParticipantId: p1.Id
        );
        _ = campaign.RecordContributionCredit(
            tx3,
            200.00m,
            "Secret Name",
            CampaignDonorPrivacy.AnonymousPublicly,
            now.AddMinutes(2),
            creditedParticipantId: p2.Id
        );
        _ = campaign.RecordContributionCredit(
            tx4,
            100.00m,
            "Staff Only Donor",
            CampaignDonorPrivacy.PrivateStaffOnly,
            now.AddMinutes(3)
        );

        // Simulate $100 partial refund on tx1 ($400 -> $300 net)
        var netMap = new Dictionary<Guid, decimal>
        {
            [tx1.Value] = 300.00m,
            [tx2.Value] = 300.00m,
            [tx3.Value] = 200.00m,
            [tx4.Value] = 100.00m,
        };

        var summary = CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap);
        Assert.Equal(900.00m, summary.TotalRaisedAmount);
        Assert.Equal(36.00m, summary.ProgressPercent);
        Assert.Equal(4, summary.DonorCount);

        var participants = CampaignProgressCalculator.ProjectParticipants(
            campaign,
            netMap,
            publicOnly: true
        );
        Assert.Equal(600.00m, participants[0].RaisedAmount);
        Assert.Equal(60.00m, participants[0].ProgressPercent);

        var publicFeed = CampaignProgressCalculator.ProjectPublicDonorFeed(campaign, netMap);
        Assert.Equal(3, publicFeed.Count);
        Assert.Contains(publicFeed, static f =>
            string.Equals(f.DisplayDonorName, "Anonymous Supporter", StringComparison.Ordinal)
            && f.VisibleAmount == 200.00m
        );
        Assert.Contains(publicFeed, static f =>
            string.Equals(f.DisplayDonorName, "Donor Two", StringComparison.Ordinal)
            && f.VisibleAmount is null
        );
        Assert.DoesNotContain(publicFeed, static f =>
            string.Equals(f.DisplayDonorName, "Staff Only Donor", StringComparison.Ordinal)
        );

        var leaderRows = CampaignProgressCalculator.ProjectLeaderContributions(campaign, netMap);
        Assert.Equal(4, leaderRows.Count);
    }
}
