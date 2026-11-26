using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to retrieve a single EngagementOpportunity by GUID or URL slug.</summary>
/// <param name="IdOrSlug">GUID or URL slug.</param>
public sealed record GetEngagementOpportunityByIdOrSlugQuery(string IdOrSlug)
    : IQuery<EngagementOpportunityResponse>;
