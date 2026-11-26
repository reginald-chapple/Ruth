namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Confirmation returned after recording a campaign contribution and participant credit.</summary>
public sealed record CampaignContributionReceiptResponse(
    Guid CreditId,
    Guid TransactionId,
    Guid CampaignId,
    Guid? CreditedParticipantId,
    decimal Amount,
    string CurrencyCode,
    CampaignDonorPrivacy DonorPrivacy,
    decimal UpdatedCampaignTotalRaised,
    decimal UpdatedCampaignProgressPercent
);
