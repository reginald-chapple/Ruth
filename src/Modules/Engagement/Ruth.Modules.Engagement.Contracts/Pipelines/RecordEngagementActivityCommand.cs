using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to log a touch/activity on an EngagementRequest.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="ActivityTypeId">Optional PipelineActivityType identifier.</param>
/// <param name="ActivityName">Activity label when ActivityTypeId is omitted.</param>
/// <param name="ConnectorPersonId">Optional actor Person identifier.</param>
/// <param name="Note">Optional activity notes.</param>
public sealed record RecordEngagementActivityCommand(
    Guid RequestId,
    Guid? ActivityTypeId,
    string? ActivityName = null,
    Guid? ConnectorPersonId = null,
    string? Note = null
) : ICommand<EngagementRequestResponse>;
