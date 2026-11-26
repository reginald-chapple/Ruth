namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Destination placement group configuration persisted as JSONB on an EngagementOpportunity.</summary>
public sealed record OpportunityPlacementGroupConfig(
    Guid GroupId,
    Guid? GroupRoleId = null,
    Guid? BranchId = null,
    bool UseAllGroupsOfType = false
);
