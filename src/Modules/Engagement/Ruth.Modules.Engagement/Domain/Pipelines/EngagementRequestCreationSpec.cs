namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Creation inputs for an EngagementRequest.</summary>
public sealed record EngagementRequestCreationSpec(
    Guid RequesterPersonId,
    Guid? BranchId = null,
    Guid? AssignedConnectorPersonId = null,
    Guid? PlacementGroupId = null,
    Guid? PlacementGroupRoleId = null,
    DateOnly? FollowUpDate = null,
    string? Comments = null
);
