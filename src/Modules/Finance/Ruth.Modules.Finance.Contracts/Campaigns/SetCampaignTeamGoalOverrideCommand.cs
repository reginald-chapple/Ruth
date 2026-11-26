using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command for a team/group leader to set or clear a team fundraising goal override when permitted.</summary>
public sealed record SetCampaignTeamGoalOverrideCommand(
    Guid CampaignId,
    decimal? TeamGoalOverrideAmount
) : ICommand<FundraisingCampaignResponse>;
