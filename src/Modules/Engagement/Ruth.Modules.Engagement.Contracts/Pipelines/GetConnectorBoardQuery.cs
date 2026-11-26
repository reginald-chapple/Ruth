using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to load the Kanban/Grid Coordinator Connector Board for a PipelineType.</summary>
/// <param name="PipelineTypeId">Target PipelineType identifier.</param>
/// <param name="EngagementOpportunityId">Optional EngagementOpportunity filter.</param>
/// <param name="BranchId">Optional Branch filter.</param>
/// <param name="AssignedConnectorPersonId">Optional coordinator Person filter.</param>
/// <param name="IncludeClosed">Whether to include Connected and Inactive requests.</param>
public sealed record GetConnectorBoardQuery(
    Guid PipelineTypeId,
    Guid? EngagementOpportunityId = null,
    Guid? BranchId = null,
    Guid? AssignedConnectorPersonId = null,
    bool IncludeClosed = false
) : IQuery<ConnectorBoardResponse>;
