using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to close an EngagementRequest as Inactive.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="Reason">Optional closure reason.</param>
/// <param name="ActorPersonId">Optional actor Person identifier.</param>
public sealed record MarkEngagementRequestInactiveCommand(
    Guid RequestId,
    string? Reason = null,
    Guid? ActorPersonId = null
) : ICommand<EngagementRequestResponse>;
