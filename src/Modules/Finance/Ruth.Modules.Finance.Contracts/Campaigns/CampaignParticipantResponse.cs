namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Participant fundraising profile, goal override, and raised total within a campaign.</summary>
public sealed record CampaignParticipantResponse(
    Guid Id,
    Guid CampaignId,
    Guid PersonId,
    Guid? GroupMemberId,
    string DisplayName,
    decimal? CustomGoalAmount,
    decimal EffectiveGoalAmount,
    decimal RaisedAmount,
    decimal ProgressPercent,
    int ContributionCount,
    string? PersonalMessage,
    bool IsFundraisingEnabled,
    bool IsPublicProfileEnabled
);
