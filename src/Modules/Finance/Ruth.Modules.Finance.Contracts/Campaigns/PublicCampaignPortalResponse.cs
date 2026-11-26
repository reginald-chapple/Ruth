namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Public contribution portal projection including campaign progress, public participants, and privacy-safe donor feed.</summary>
public sealed record PublicCampaignPortalResponse(
    Guid CampaignId,
    string Name,
    string Slug,
    string? Summary,
    string? PublicDescription,
    string CurrencyCode,
    decimal EffectiveGoalAmount,
    decimal TotalRaisedAmount,
    decimal ProgressPercent,
    int DonorCount,
    bool AcceptingContributions,
    Guid? DefaultFinancialGatewayId,
    IReadOnlyList<CampaignParticipantResponse> PublicParticipants,
    IReadOnlyList<PublicCampaignDonorFeedItemDto> RecentPublicContributions
);
