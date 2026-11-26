namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Summary and progress projection for a configured fundraising campaign.</summary>
public sealed record FundraisingCampaignResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid GroupId,
    Guid FinancialAccountId,
    string CurrencyCode,
    decimal BaseCampaignGoalAmount,
    decimal? TeamGoalOverrideAmount,
    decimal EffectiveTeamGoalAmount,
    decimal? DefaultParticipantGoalAmount,
    decimal TotalRaisedAmount,
    decimal ProgressPercent,
    int DonorCount,
    int ParticipantCount,
    string? Summary,
    string? PublicDescription,
    bool AllowTeamGoalOverride,
    bool AllowParticipantGoalOverride,
    bool AllowPublicContributions,
    Guid? DefaultFinancialGatewayId,
    Guid? BranchId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    FundraisingCampaignStatus Status,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
