namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Combined Kanban and Grid projection for the Coordinator Connector Board.</summary>
/// <param name="PipelineTypeId">PipelineType identifier.</param>
/// <param name="PipelineName">Pipeline display name.</param>
/// <param name="TotalActiveCount">Count of Active requests matching filter.</param>
/// <param name="IdleCount">Count of Idle or OverdueFollowUp requests.</param>
/// <param name="FutureFollowUpCount">Count of FutureFollowUp requests.</param>
/// <param name="ConnectedCount">Count of Connected requests.</param>
/// <param name="Columns">Ordered Kanban stage columns.</param>
/// <param name="FlatItems">Flat list of requests for Grid view.</param>
public sealed record ConnectorBoardResponse(
    Guid PipelineTypeId,
    string PipelineName,
    int TotalActiveCount,
    int IdleCount,
    int FutureFollowUpCount,
    int ConnectedCount,
    IReadOnlyList<ConnectorBoardColumnDto> Columns,
    IReadOnlyList<EngagementRequestResponse> FlatItems
);
