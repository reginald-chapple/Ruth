using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Query to load a public contribution portal by campaign slug with donor privacy rules enforced.</summary>
public sealed record GetPublicCampaignPortalQuery(string Slug)
    : IQuery<PublicCampaignPortalResponse>;
