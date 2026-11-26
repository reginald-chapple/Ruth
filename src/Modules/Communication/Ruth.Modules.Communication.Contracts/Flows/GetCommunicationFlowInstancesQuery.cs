using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Lists CommunicationFlowInstance cohort runs and enrolled recipient progressions for a flow.</summary>
/// <param name="FlowId">Target flow identifier.</param>
public sealed record GetCommunicationFlowInstancesQuery(Guid FlowId)
    : IQuery<IReadOnlyList<CommunicationFlowInstanceResponse>>;
