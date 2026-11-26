using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Campaigns;

internal static class FinanceCampaignErrors
{
    public static readonly DomainError CampaignInvalid = DomainError.Validation(
        "Finance.Campaign.Invalid",
        "Fundraising campaign requires a valid name, URL slug, group, financial account, positive goal, and valid date window."
    );

    public static readonly DomainError CampaignSlugDuplicate = DomainError.Conflict(
        "Finance.Campaign.SlugDuplicate",
        "Another fundraising campaign already uses this URL slug."
    );

    public static readonly DomainError CampaignNotFound = DomainError.NotFound(
        "Finance.Campaign.NotFound",
        "The requested fundraising campaign was not found."
    );

    public static readonly DomainError CampaignClosedForContributions = DomainError.Conflict(
        "Finance.Campaign.Closed",
        "This fundraising campaign is not currently open to public contributions."
    );

    public static readonly DomainError TeamGoalOverrideNotAllowed = DomainError.Conflict(
        "Finance.Campaign.TeamGoalOverrideDisabled",
        "Team goal overrides are disabled on this fundraising campaign."
    );

    public static readonly DomainError ParticipantGoalOverrideNotAllowed = DomainError.Conflict(
        "Finance.Campaign.ParticipantGoalOverrideDisabled",
        "Participant goal overrides are disabled on this fundraising campaign."
    );

    public static readonly DomainError ParticipantInvalid = DomainError.Validation(
        "Finance.CampaignParticipant.Invalid",
        "Campaign participant requires a valid Person identifier, display name, and positive custom goal when overridden."
    );

    public static readonly DomainError ParticipantDuplicate = DomainError.Conflict(
        "Finance.CampaignParticipant.Duplicate",
        "This constituent is already enrolled in the fundraising campaign."
    );

    public static readonly DomainError ParticipantNotFound = DomainError.NotFound(
        "Finance.CampaignParticipant.NotFound",
        "The specified campaign participant was not found or has fundraising disabled."
    );

    public static readonly DomainError ContributionDonorNameRequired = DomainError.Validation(
        "Finance.CampaignContribution.DonorNameRequired",
        "A donor display name is required when recording a campaign contribution."
    );
}
