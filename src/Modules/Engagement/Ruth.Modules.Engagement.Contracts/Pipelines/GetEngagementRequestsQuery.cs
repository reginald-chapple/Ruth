using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to list EngagementRequests filtered by person, coordinator, opportunity, or state.</summary>
/// <param name="PipelineTypeId">Optional PipelineType filter.</param>
/// <param name="EngagementOpportunityId">Optional Opportunity filter.</param>
/// <param name="RequesterPersonId">Optional requester Person filter.</param>
/// <param name="AssignedConnectorPersonId">Optional coordinator Person filter.</param>
/// <param name="State">Optional lifecycle state filter.</param>
public sealed record GetEngagementRequestsQuery(
    Guid? PipelineTypeId = null,
    Guid? EngagementOpportunityId = null,
    Guid? RequesterPersonId = null,
    Guid? AssignedConnectorPersonId = null,
    EngagementRequestState? State = null
) : IQuery<IReadOnlyList<EngagementRequestResponse>>;
