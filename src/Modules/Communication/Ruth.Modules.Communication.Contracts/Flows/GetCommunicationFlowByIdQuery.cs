using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Retrieves a single CommunicationFlow definition by identifier.</summary>
/// <param name="FlowId">Unique flow identifier.</param>
public sealed record GetCommunicationFlowByIdQuery(Guid FlowId) : IQuery<CommunicationFlowResponse>;
