using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to synchronize active members of the campaign's linked Group into campaign participant profiles.</summary>
public sealed record SyncGroupMembersToCampaignCommand(Guid CampaignId)
    : ICommand<IReadOnlyList<CampaignParticipantResponse>>;
