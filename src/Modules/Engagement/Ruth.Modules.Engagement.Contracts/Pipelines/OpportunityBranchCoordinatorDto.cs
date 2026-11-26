namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Branch-scoped coordinator routing configuration for an EngagementOpportunity.</summary>
/// <param name="BranchId">Optional Core Branch identifier.</param>
/// <param name="DefaultConnectorPersonId">Optional default coordinator person for this branch.</param>
/// <param name="ConnectorGroupId">Optional coordinator pool group identifier.</param>
public sealed record OpportunityBranchCoordinatorDto(
    Guid? BranchId,
    Guid? DefaultConnectorPersonId,
    Guid? ConnectorGroupId = null
);
