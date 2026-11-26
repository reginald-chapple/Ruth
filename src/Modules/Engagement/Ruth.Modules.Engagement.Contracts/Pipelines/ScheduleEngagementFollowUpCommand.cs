using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to place an EngagementRequest into FutureFollowUp until a scheduled date.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="FollowUpDate">Scheduled follow-up calendar date.</param>
/// <param name="ActorPersonId">Optional actor Person identifier.</param>
/// <param name="Note">Optional follow-up note.</param>
public sealed record ScheduleEngagementFollowUpCommand(
    Guid RequestId,
    DateOnly FollowUpDate,
    Guid? ActorPersonId = null,
    string? Note = null
) : ICommand<EngagementRequestResponse>;
