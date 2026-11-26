using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Lists registered communication transport providers and their configuration readiness.</summary>
/// <param name="Channel">Optional channel filter.</param>
public sealed record GetCommunicationTransportsQuery(CommunicationChannel? Channel = null)
    : IQuery<IReadOnlyList<CommunicationTransportStatusResponse>>;
