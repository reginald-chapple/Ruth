namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Declarative action executed automatically when an EngagementRequest enters a PipelineStage.</summary>
public enum StageAutomationKind
{
    /// <summary>No automated side effect.</summary>
    None = 0,

    /// <summary>Places the constituent into the configured destination group.</summary>
    AddToPlacementGroup = 1,

    /// <summary>Dispatches a transactional SystemCommunication notification.</summary>
    SendSystemNotification = 2,

    /// <summary>Transfers the request to another EngagementOpportunity.</summary>
    TransferToOpportunity = 3,
}
