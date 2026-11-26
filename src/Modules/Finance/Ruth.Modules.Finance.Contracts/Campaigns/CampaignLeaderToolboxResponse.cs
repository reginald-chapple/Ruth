namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Workspace view for campaign/team leaders containing full roster progress and contribution credits.</summary>
public sealed record CampaignLeaderToolboxResponse(
    FundraisingCampaignResponse Campaign,
    IReadOnlyList<CampaignParticipantResponse> Participants,
    IReadOnlyList<LeaderCampaignContributionRowDto> Contributions
);
