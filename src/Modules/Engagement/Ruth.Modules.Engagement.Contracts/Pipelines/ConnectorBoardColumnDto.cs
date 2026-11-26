namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Kanban column projection for a single PipelineStage on the Connector Board.</summary>
/// <param name="StageId">Stage identifier.</param>
/// <param name="StageName">Stage display name.</param>
/// <param name="Order">Stage order.</param>
/// <param name="IsTerminalCompleted">Whether this column represents a terminal Connected stage.</param>
/// <param name="IsTerminalInactive">Whether this column represents a terminal Inactive stage.</param>
/// <param name="Requests">Requests currently placed in this stage column.</param>
public sealed record ConnectorBoardColumnDto(
    Guid StageId,
    string StageName,
    int Order,
    bool IsTerminalCompleted,
    bool IsTerminalInactive,
    IReadOnlyList<EngagementRequestResponse> Requests
);
