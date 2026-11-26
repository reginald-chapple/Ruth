using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Contracts.Events;

/// <summary>Outbox integration event emitted when a contribution is credited to a fundraising campaign.</summary>
public sealed record CampaignContributionRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CampaignId,
    Guid CreditId,
    Guid TransactionId,
    Guid? CreditedParticipantId,
    Guid? DonorPersonId,
    decimal Amount,
    string CurrencyCode,
    CampaignDonorPrivacy DonorPrivacy
) : IIntegrationEvent;
