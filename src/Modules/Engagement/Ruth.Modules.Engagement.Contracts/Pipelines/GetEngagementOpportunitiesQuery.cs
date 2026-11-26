using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to list EngagementOpportunities filtered by pipeline, branch, or visibility.</summary>
/// <param name="PipelineTypeId">Optional PipelineType filter.</param>
/// <param name="BranchId">Optional Branch filter.</param>
/// <param name="ActiveOnly">Whether to include only active opportunities.</param>
/// <param name="PublicOnly">Whether to include only public Opportunity Finder items.</param>
/// <param name="SearchText">Optional keyword search filter.</param>
public sealed record GetEngagementOpportunitiesQuery(
    Guid? PipelineTypeId = null,
    Guid? BranchId = null,
    bool ActiveOnly = false,
    bool PublicOnly = false,
    string? SearchText = null
) : IQuery<IReadOnlyList<EngagementOpportunityResponse>>;
