using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Fundraising participant profile within a <see cref="FundraisingCampaign" /> supporting personal goal overrides.</summary>
public sealed class CampaignParticipant : Entity<CampaignParticipantId>
{
    private CampaignParticipant() { }

    internal CampaignParticipant(
        CampaignParticipantId id,
        FundraisingCampaignId campaignId,
        Guid personId,
        Guid? groupMemberId,
        string displayName,
        decimal? customGoalAmount,
        string? personalMessage,
        bool isFundraisingEnabled,
        bool isPublicProfileEnabled
    )
        : base(id)
    {
        CampaignId = campaignId;
        PersonId = personId;
        GroupMemberId = groupMemberId;
        DisplayName = displayName;
        CustomGoalAmount = customGoalAmount;
        PersonalMessage = personalMessage;
        IsFundraisingEnabled = isFundraisingEnabled;
        IsPublicProfileEnabled = isPublicProfileEnabled;
    }

    public FundraisingCampaignId CampaignId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid? GroupMemberId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public decimal? CustomGoalAmount { get; private set; }
    public string? PersonalMessage { get; private set; }
    public bool IsFundraisingEnabled { get; private set; } = true;
    public bool IsPublicProfileEnabled { get; private set; } = true;

    /// <summary>Resolves the effective participant goal from personal override or campaign default.</summary>
    public decimal GetEffectiveGoal(decimal? campaignDefaultParticipantGoal)
    {
        return CustomGoalAmount ?? campaignDefaultParticipantGoal ?? 0m;
    }

    internal Result UpdateGoalAndProfile(
        decimal? customGoalAmount,
        bool allowGoalOverride,
        int currencyScale,
        string? personalMessage,
        bool isFundraisingEnabled,
        bool isPublicProfileEnabled
    )
    {
        if (customGoalAmount.HasValue)
        {
            if (!allowGoalOverride)
            {
                return FinanceCampaignErrors.ParticipantGoalOverrideNotAllowed;
            }

            if (customGoalAmount.Value <= 0m)
            {
                return FinanceCampaignErrors.ParticipantInvalid;
            }

            CustomGoalAmount = decimal.Round(
                customGoalAmount.Value,
                currencyScale,
                MidpointRounding.AwayFromZero
            );
        }
        else
        {
            CustomGoalAmount = null;
        }

        PersonalMessage = string.IsNullOrWhiteSpace(personalMessage)
            ? null
            : personalMessage.Trim();
        IsFundraisingEnabled = isFundraisingEnabled;
        IsPublicProfileEnabled = isPublicProfileEnabled;
        return Result.Success();
    }

    internal bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
