using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Lists configured CommunicationFlow drip campaign definitions.</summary>
/// <param name="Category">Optional flow category filter.</param>
/// <param name="ActiveOnly">Whether to return only active flows.</param>
public sealed record GetCommunicationFlowsQuery(
    CommunicationFlowCategory? Category = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<CommunicationFlowResponse>>;
