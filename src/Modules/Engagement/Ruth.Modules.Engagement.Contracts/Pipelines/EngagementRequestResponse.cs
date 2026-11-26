namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Represents an EngagementRequest on the Connector Board or constituent history.</summary>
/// <param name="Id">Request identifier.</param>
/// <param name="PipelineTypeId">Owning PipelineType identifier.</param>
/// <param name="PipelineName">Owning PipelineType display name.</param>
/// <param name="EngagementOpportunityId">Owning EngagementOpportunity identifier.</param>
/// <param name="OpportunityName">Opportunity display name.</param>
/// <param name="RequesterPersonId">Requester CRM Person identifier.</param>
/// <param name="RequesterDisplayName">Requester display name.</param>
/// <param name="RequesterEmail">Optional requester email.</param>
/// <param name="AssignedConnectorPersonId">Optional assigned coordinator Person identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="CurrentStageId">Current PipelineStage identifier.</param>
/// <param name="CurrentStageName">Current PipelineStage display name.</param>
/// <param name="PlacementGroupId">Optional assigned placement Group identifier.</param>
/// <param name="PlacementGroupRoleId">Optional placement GroupRole identifier.</param>
/// <param name="RequestState">Current lifecycle state.</param>
/// <param name="Health">Computed SLA health indicator.</param>
/// <param name="DaysIdle">Days elapsed since last activity.</param>
/// <param name="FollowUpDate">Optional scheduled follow-up date.</param>
/// <param name="Comments">Intake or coordinator summary comments.</param>
/// <param name="LastActivityAtUtc">UTC timestamp of last activity.</param>
/// <param name="ConnectedAtUtc">Optional UTC timestamp when connected.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
/// <param name="Activities">Chronological activity log.</param>
public sealed record EngagementRequestResponse(
    Guid Id,
    Guid PipelineTypeId,
    string PipelineName,
    Guid EngagementOpportunityId,
    string OpportunityName,
    Guid RequesterPersonId,
    string RequesterDisplayName,
    string? RequesterEmail,
    Guid? AssignedConnectorPersonId,
    Guid? BranchId,
    Guid CurrentStageId,
    string CurrentStageName,
    Guid? PlacementGroupId,
    Guid? PlacementGroupRoleId,
    EngagementRequestState RequestState,
    EngagementRequestHealth Health,
    int DaysIdle,
    DateOnly? FollowUpDate,
    string? Comments,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset? ConnectedAtUtc,
    DateTimeOffset CreatedOnUtc,
    IReadOnlyList<EngagementRequestActivityResponse> Activities
);
