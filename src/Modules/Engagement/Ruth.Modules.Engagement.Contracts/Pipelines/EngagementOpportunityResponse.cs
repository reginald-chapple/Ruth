namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Represents an EngagementOpportunity for volunteer intake, onboarding, mentorship, or referrals.</summary>
/// <param name="Id">Opportunity identifier.</param>
/// <param name="PipelineTypeId">Owning PipelineType identifier.</param>
/// <param name="PipelineName">Owning PipelineType name.</param>
/// <param name="Name">Opportunity name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="PublicSummary">Short public summary.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="IconCssClass">Optional icon class.</param>
/// <param name="PhotoBinaryFileId">Optional photo BinaryFile ID.</param>
/// <param name="DefaultBranchId">Optional default Branch ID.</param>
/// <param name="DefaultConnectorPersonId">Optional fallback coordinator person ID.</param>
/// <param name="BranchConfigs">Branch coordinator routing rules.</param>
/// <param name="PlacementGroupConfigs">Placement group configurations.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="IsPublic">Whether visible publicly.</param>
/// <param name="Attributes">Extensible attributes.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record EngagementOpportunityResponse(
    Guid Id,
    Guid PipelineTypeId,
    string PipelineName,
    string Name,
    string Slug,
    string? PublicSummary,
    string? Description,
    string? IconCssClass,
    Guid? PhotoBinaryFileId,
    Guid? DefaultBranchId,
    Guid? DefaultConnectorPersonId,
    IReadOnlyList<OpportunityBranchCoordinatorDto> BranchConfigs,
    IReadOnlyList<OpportunityPlacementGroupDto> PlacementGroupConfigs,
    bool IsActive,
    bool IsPublic,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc
);
