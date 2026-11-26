using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to update a participant's personal fundraising goal override, story, or profile visibility.</summary>
public sealed record UpdateCampaignParticipantGoalCommand(
    Guid CampaignId,
    Guid ParticipantId,
    decimal? CustomGoalAmount,
    string? PersonalMessage = null,
    bool IsFundraisingEnabled = true,
    bool IsPublicProfileEnabled = true
) : ICommand<CampaignParticipantResponse>;
