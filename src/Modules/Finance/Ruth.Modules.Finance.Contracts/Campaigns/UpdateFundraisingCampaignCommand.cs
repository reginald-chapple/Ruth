using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to update an existing fundraising campaign configuration.</summary>
public sealed record UpdateFundraisingCampaignCommand(Guid CampaignId, FundraisingCampaignSpec Spec)
    : ICommand<FundraisingCampaignResponse>;
