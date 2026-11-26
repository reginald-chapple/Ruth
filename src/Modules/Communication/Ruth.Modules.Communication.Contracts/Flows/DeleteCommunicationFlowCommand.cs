using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Deletes a non-system CommunicationFlow definition when no instances exist.</summary>
/// <param name="FlowId">Target flow identifier.</param>
public sealed record DeleteCommunicationFlowCommand(Guid FlowId) : ICommand<bool>;
