namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Destination placement group configuration for an EngagementOpportunity.</summary>
/// <param name="GroupId">Target Groups module Group identifier.</param>
/// <param name="GroupRoleId">Optional target GroupTypeRole identifier.</param>
/// <param name="BranchId">Optional branch filter.</param>
/// <param name="UseAllGroupsOfType">Whether any group of the target group's type may satisfy placement.</param>
public sealed record OpportunityPlacementGroupDto(
    Guid GroupId,
    Guid? GroupRoleId = null,
    Guid? BranchId = null,
    bool UseAllGroupsOfType = false
);
