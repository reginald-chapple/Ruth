namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Privacy-filtered contribution entry safe for rendering on public fundraising portals.</summary>
public sealed record PublicCampaignDonorFeedItemDto(
    Guid CreditId,
    string DisplayDonorName,
    decimal? VisibleAmount,
    string CurrencyCode,
    string? CreditedParticipantDisplayName,
    string? DonorComment,
    DateTimeOffset ContributedAtUtc
);
