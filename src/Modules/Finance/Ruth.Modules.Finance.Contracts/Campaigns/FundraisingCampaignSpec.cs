namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Specification for creating or updating a Group + Financial Account fundraising campaign.</summary>
public sealed record FundraisingCampaignSpec(
    string Name,
    string Slug,
    Guid GroupId,
    Guid FinancialAccountId,
    decimal CampaignGoalAmount,
    decimal? DefaultParticipantGoalAmount = null,
    string CurrencyCode = "USD",
    string? Summary = null,
    string? PublicDescription = null,
    decimal? TeamGoalOverrideAmount = null,
    bool AllowTeamGoalOverride = true,
    bool AllowParticipantGoalOverride = true,
    bool AllowPublicContributions = true,
    Guid? DefaultFinancialGatewayId = null,
    Guid? BranchId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    FundraisingCampaignStatus Status = FundraisingCampaignStatus.Active,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
