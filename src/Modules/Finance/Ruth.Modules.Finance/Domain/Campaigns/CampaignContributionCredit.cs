using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Links a settled <see cref="FinancialTransaction" /> to a <see cref="FundraisingCampaign" /> with donor privacy and optional participant credit.</summary>
public sealed class CampaignContributionCredit : Entity<CampaignContributionCreditId>
{
    private CampaignContributionCredit() { }

    internal CampaignContributionCredit(
        CampaignContributionCreditId id,
        FundraisingCampaignId campaignId,
        FinancialTransactionId transactionId,
        CampaignParticipantId? creditedParticipantId,
        Guid? donorPersonId,
        string donorDisplayName,
        decimal amount,
        string currencyCode,
        CampaignDonorPrivacy donorPrivacy,
        string? donorComment,
        DateTimeOffset contributedAtUtc
    )
        : base(id)
    {
        CampaignId = campaignId;
        TransactionId = transactionId;
        CreditedParticipantId = creditedParticipantId;
        DonorPersonId = donorPersonId;
        DonorDisplayName = donorDisplayName;
        Amount = amount;
        CurrencyCode = currencyCode;
        DonorPrivacy = donorPrivacy;
        DonorComment = donorComment;
        ContributedAtUtc = contributedAtUtc.ToUniversalTime();
    }

    public FundraisingCampaignId CampaignId { get; private set; }
    public FinancialTransactionId TransactionId { get; private set; }
    public CampaignParticipantId? CreditedParticipantId { get; private set; }
    public Guid? DonorPersonId { get; private set; }
    public string DonorDisplayName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public CampaignDonorPrivacy DonorPrivacy { get; private set; }
    public string? DonorComment { get; private set; }
    public DateTimeOffset ContributedAtUtc { get; private set; }

    internal bool RepointDonorPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (DonorPersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        DonorPersonId = survivingPersonId;
        return true;
    }
}
