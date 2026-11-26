using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to configure a peer-to-peer, team, or project fundraising campaign linked to a Group and Financial Account.</summary>
public sealed record CreateFundraisingCampaignCommand(FundraisingCampaignSpec Spec)
    : ICommand<FundraisingCampaignResponse>;
