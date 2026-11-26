namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Branch coordinator routing rule persisted as JSONB on an EngagementOpportunity.</summary>
public sealed record OpportunityBranchCoordinatorConfig(
    Guid? BranchId,
    Guid? DefaultConnectorPersonId,
    Guid? ConnectorGroupId = null
);
