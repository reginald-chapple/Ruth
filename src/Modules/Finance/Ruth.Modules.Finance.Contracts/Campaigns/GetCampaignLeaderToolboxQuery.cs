using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Query to load the Campaign Leader Toolbox view including full participant goals and contribution rows.</summary>
public sealed record GetCampaignLeaderToolboxQuery(Guid CampaignId)
    : IQuery<CampaignLeaderToolboxResponse>;
