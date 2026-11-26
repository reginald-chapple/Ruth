using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to move an EngagementRequest to a target PipelineStage and run stage automations.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="TargetStageId">Destination PipelineStage identifier.</param>
/// <param name="ActorPersonId">Optional actor Person identifier.</param>
/// <param name="Note">Optional transition note.</param>
public sealed record TransitionEngagementRequestStageCommand(
    Guid RequestId,
    Guid TargetStageId,
    Guid? ActorPersonId = null,
    string? Note = null
) : ICommand<EngagementRequestResponse>;
