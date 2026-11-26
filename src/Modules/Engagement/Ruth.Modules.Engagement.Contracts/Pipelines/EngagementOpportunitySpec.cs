namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Specification for creating or updating an EngagementOpportunity.</summary>
/// <param name="PipelineTypeId">Owning PipelineType identifier.</param>
/// <param name="Name">Opportunity display name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="PublicSummary">Short public summary for the Opportunity Finder.</param>
/// <param name="Description">Detailed opportunity description.</param>
/// <param name="IconCssClass">Optional icon class.</param>
/// <param name="PhotoBinaryFileId">Optional Core BinaryFile photo identifier.</param>
/// <param name="DefaultBranchId">Optional default Branch identifier.</param>
/// <param name="DefaultConnectorPersonId">Optional fallback coordinator person identifier.</param>
/// <param name="BranchConfigs">Branch-specific coordinator routing configurations.</param>
/// <param name="PlacementGroupConfigs">Allowed destination placement group configurations.</param>
/// <param name="IsActive">Whether the opportunity accepts requests.</param>
/// <param name="IsPublic">Whether the opportunity appears in the public Opportunity Finder.</param>
/// <param name="Attributes">Optional extensible attribute values.</param>
public sealed record EngagementOpportunitySpec(
    Guid PipelineTypeId,
    string Name,
    string Slug,
    string? PublicSummary = null,
    string? Description = null,
    string? IconCssClass = null,
    Guid? PhotoBinaryFileId = null,
    Guid? DefaultBranchId = null,
    Guid? DefaultConnectorPersonId = null,
    IReadOnlyList<OpportunityBranchCoordinatorDto>? BranchConfigs = null,
    IReadOnlyList<OpportunityPlacementGroupDto>? PlacementGroupConfigs = null,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
