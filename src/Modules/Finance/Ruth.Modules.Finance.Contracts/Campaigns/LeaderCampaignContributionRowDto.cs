namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Detailed contribution credit row visible in the authorized Campaign Leader Toolbox.</summary>
public sealed record LeaderCampaignContributionRowDto(
    Guid CreditId,
    Guid TransactionId,
    string DonorDisplayName,
    CampaignDonorPrivacy DonorPrivacy,
    decimal NetAmount,
    string CurrencyCode,
    Guid? CreditedParticipantId,
    string? CreditedParticipantName,
    string? DonorComment,
    DateTimeOffset ContributedAtUtc
);
