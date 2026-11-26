namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Result of enqueuing or flushing buffered interaction telemetry records.</summary>
public sealed record InteractionBufferStatusResponse(
    int EnqueuedCount,
    int PendingApproximateCount,
    long TotalFlushedCount
);
