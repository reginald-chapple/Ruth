namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Logged activity entry on an EngagementRequest.</summary>
/// <param name="Id">Activity entry identifier.</param>
/// <param name="ActivityTypeId">Optional PipelineActivityType identifier.</param>
/// <param name="ActivityName">Activity label.</param>
/// <param name="ConnectorPersonId">Optional actor/coordinator person identifier.</param>
/// <param name="Note">Activity note.</param>
/// <param name="OccurredAtUtc">UTC timestamp of activity.</param>
public sealed record EngagementRequestActivityResponse(
    Guid Id,
    Guid? ActivityTypeId,
    string ActivityName,
    Guid? ConnectorPersonId,
    string? Note,
    DateTimeOffset OccurredAtUtc
);
