using Ruth.Modules.Finance.Contracts.Campaigns;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Pure clean-room calculator computing campaign/participant progress percentages and donor-privacy projections.</summary>
public static class CampaignProgressCalculator
{
    /// <summary>Computes percentage progress toward a positive goal.</summary>
    public static decimal CalculateProgressPercent(decimal raisedAmount, decimal goalAmount)
    {
        return goalAmount <= 0m || raisedAmount <= 0m
            ? 0m
            : decimal.Round(raisedAmount / goalAmount * 100m, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Projects campaign summary metrics using net settled transaction amounts.</summary>
    public static FundraisingCampaignResponse ProjectCampaignSummary(
        FundraisingCampaign campaign,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts
    )
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(netTransactionAmounts);

        var totalRaised = 0m;
        var activeCount = 0;
        foreach (var credit in campaign.Credits)
        {
            var net = ResolveNetCreditAmount(credit, netTransactionAmounts);
            if (net > 0m)
            {
                totalRaised += net;
                activeCount++;
            }
        }

        var effectiveGoal = campaign.EffectiveTeamGoalAmount;
        return new FundraisingCampaignResponse(
            campaign.Id.Value,
            campaign.Name,
            campaign.Slug,
            campaign.GroupId,
            campaign.FinancialAccountId.Value,
            campaign.CurrencyCode,
            campaign.CampaignGoalAmount,
            campaign.TeamGoalOverrideAmount,
            effectiveGoal,
            campaign.DefaultParticipantGoalAmount,
            totalRaised,
            CalculateProgressPercent(totalRaised, effectiveGoal),
            activeCount,
            campaign.Participants.Count,
            campaign.Summary,
            campaign.PublicDescription,
            campaign.AllowTeamGoalOverride,
            campaign.AllowParticipantGoalOverride,
            campaign.AllowPublicContributions,
            campaign.DefaultFinancialGatewayId?.Value,
            campaign.BranchId,
            campaign.StartDate,
            campaign.EndDate,
            campaign.Status,
            campaign.CreatedOnUtc,
            campaign.ModifiedOnUtc
        );
    }

    /// <summary>Projects participant leaderboard and personal goal progress.</summary>
    public static IReadOnlyList<CampaignParticipantResponse> ProjectParticipants(
        FundraisingCampaign campaign,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts,
        bool publicOnly
    )
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(netTransactionAmounts);

        var filtered = publicOnly
            ? campaign.Participants.Where(static p =>
                p.IsFundraisingEnabled && p.IsPublicProfileEnabled
            )
            : campaign.Participants;

        return
        [
            .. filtered
                .Select(p => BuildParticipantRow(p, campaign, netTransactionAmounts))
                .OrderByDescending(static p => p.RaisedAmount)
                .ThenBy(static p => p.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <summary>Projects privacy-filtered contribution feed items safe for anonymous public display.</summary>
    public static IReadOnlyList<PublicCampaignDonorFeedItemDto> ProjectPublicDonorFeed(
        FundraisingCampaign campaign,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts,
        int maxItems = 50
    )
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(netTransactionAmounts);

        var participantNames = campaign.Participants.ToDictionary(
            static p => p.Id,
            static p => p.DisplayName
        );
        var items = new List<PublicCampaignDonorFeedItemDto>();

        foreach (
            var credit in campaign
                .Credits.OrderByDescending(static c => c.ContributedAtUtc)
                .Take(Math.Clamp(maxItems, 1, 200))
        )
        {
            var net = ResolveNetCreditAmount(credit, netTransactionAmounts);
            if (net <= 0m || credit.DonorPrivacy == CampaignDonorPrivacy.PrivateStaffOnly)
            {
                continue;
            }

            var displayDonor =
                credit.DonorPrivacy == CampaignDonorPrivacy.AnonymousPublicly
                    ? "Anonymous Supporter"
                    : credit.DonorDisplayName;
            decimal? visibleAmount =
                credit.DonorPrivacy == CampaignDonorPrivacy.PublicNameHideAmount ? null : net;
            var creditedName =
                credit.CreditedParticipantId is { } pId
                && participantNames.TryGetValue(pId, out var name)
                    ? name
                    : null;

            items.Add(
                new PublicCampaignDonorFeedItemDto(
                    credit.Id.Value,
                    displayDonor,
                    visibleAmount,
                    credit.CurrencyCode,
                    creditedName,
                    credit.DonorComment,
                    credit.ContributedAtUtc
                )
            );
        }

        return items;
    }

    /// <summary>Projects complete contribution rows for the authorized Campaign Leader Toolbox.</summary>
    public static IReadOnlyList<LeaderCampaignContributionRowDto> ProjectLeaderContributions(
        FundraisingCampaign campaign,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts
    )
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(netTransactionAmounts);

        var participantNames = campaign.Participants.ToDictionary(
            static p => p.Id,
            static p => p.DisplayName
        );

        return
        [
            .. campaign
                .Credits.OrderByDescending(static c => c.ContributedAtUtc)
                .Select(c => new LeaderCampaignContributionRowDto(
                    c.Id.Value,
                    c.TransactionId.Value,
                    c.DonorDisplayName,
                    c.DonorPrivacy,
                    ResolveNetCreditAmount(c, netTransactionAmounts),
                    c.CurrencyCode,
                    c.CreditedParticipantId?.Value,
                    c.CreditedParticipantId is { } pId
                    && participantNames.TryGetValue(pId, out var name)
                        ? name
                        : null,
                    c.DonorComment,
                    c.ContributedAtUtc
                )),
        ];
    }

    private static CampaignParticipantResponse BuildParticipantRow(
        CampaignParticipant participant,
        FundraisingCampaign campaign,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts
    )
    {
        var raised = 0m;
        var count = 0;
        foreach (
            var credit in campaign.Credits.Where(c => c.CreditedParticipantId == participant.Id)
        )
        {
            var net = ResolveNetCreditAmount(credit, netTransactionAmounts);
            if (net > 0m)
            {
                raised += net;
                count++;
            }
        }

        var effectiveGoal = participant.GetEffectiveGoal(campaign.DefaultParticipantGoalAmount);
        return new CampaignParticipantResponse(
            participant.Id.Value,
            campaign.Id.Value,
            participant.PersonId,
            participant.GroupMemberId,
            participant.DisplayName,
            participant.CustomGoalAmount,
            effectiveGoal,
            raised,
            CalculateProgressPercent(raised, effectiveGoal),
            count,
            participant.PersonalMessage,
            participant.IsFundraisingEnabled,
            participant.IsPublicProfileEnabled
        );
    }

    private static decimal ResolveNetCreditAmount(
        CampaignContributionCredit credit,
        IReadOnlyDictionary<Guid, decimal> netTransactionAmounts
    )
    {
        return netTransactionAmounts.TryGetValue(credit.TransactionId.Value, out var net)
            ? Math.Min(credit.Amount, Math.Max(0m, net))
            : credit.Amount;
    }
}
