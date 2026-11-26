using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to enroll a constituent or group member as a campaign fundraiser.</summary>
public sealed record EnrollCampaignParticipantCommand(
    Guid CampaignId,
    Guid PersonId,
    string? DisplayName = null,
    Guid? GroupMemberId = null,
    decimal? CustomGoalAmount = null,
    string? PersonalMessage = null,
    bool IsPublicProfileEnabled = true
) : ICommand<CampaignParticipantResponse>;
