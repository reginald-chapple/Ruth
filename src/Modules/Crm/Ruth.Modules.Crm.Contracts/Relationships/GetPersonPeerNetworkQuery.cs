using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Query to list directed peer-network edges originating from a person.</summary>
public sealed record GetPersonPeerNetworkQuery(Guid PersonId)
    : IQuery<IReadOnlyList<PeerNetworkEdgeResponse>>;
