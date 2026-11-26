using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Command to submit a contribution through a campaign portal with donor privacy and optional participant credit.</summary>
public sealed record SubmitCampaignContributionCommand(
    string CampaignIdOrSlug,
    decimal Amount,
    string CurrencyCode,
    string DonorDisplayName,
    CampaignDonorPrivacy DonorPrivacy,
    Guid? CreditedParticipantId = null,
    Guid? DonorPersonId = null,
    Guid? FinancialGatewayId = null,
    Guid? SavedPaymentMethodId = null,
    string? OneTimeHostedToken = null,
    FinancialTenderType TenderType = FinancialTenderType.CreditCard,
    string? DonorComment = null
) : ICommand<CampaignContributionReceiptResponse>;
