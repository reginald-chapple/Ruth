using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Computes funnel, engagement, and conversion performance analytics for a CommunicationFlow.</summary>
/// <param name="FlowId">Target flow identifier.</param>
public sealed record GetCommunicationFlowAnalyticsQuery(Guid FlowId)
    : IQuery<CommunicationFlowAnalyticsResponse>;
