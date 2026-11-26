using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to assign or reassign a coordinator on an EngagementRequest.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="ConnectorPersonId">Optional assigned coordinator Person identifier.</param>
/// <param name="Note">Optional handoff note.</param>
public sealed record AssignEngagementConnectorCommand(
    Guid RequestId,
    Guid? ConnectorPersonId,
    string? Note = null
) : ICommand<EngagementRequestResponse>;
