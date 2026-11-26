using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Query to list fundraising campaigns filtered optionally by group, financial account, or status.</summary>
public sealed record GetFundraisingCampaignsQuery(
    Guid? GroupId = null,
    Guid? FinancialAccountId = null,
    FundraisingCampaignStatus? Status = null
) : IQuery<IReadOnlyList<FundraisingCampaignResponse>>;
