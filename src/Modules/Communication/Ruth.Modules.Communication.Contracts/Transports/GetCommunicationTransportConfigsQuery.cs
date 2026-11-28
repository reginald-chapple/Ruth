using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Lists masked per-tenant communication transport configurations.</summary>
/// <param name="Channel">Optional channel filter.</param>
public sealed record GetCommunicationTransportConfigsQuery(CommunicationChannel? Channel = null)
    : IQuery<IReadOnlyList<CommunicationTransportConfigResponse>>;
