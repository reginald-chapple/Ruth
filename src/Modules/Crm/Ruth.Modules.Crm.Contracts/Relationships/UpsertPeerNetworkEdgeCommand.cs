using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Command to upsert a directed peer-network connection edge with Rock-parity scoring.</summary>
public sealed record UpsertPeerNetworkEdgeCommand(
    Guid SourcePersonId,
    Guid TargetPersonId,
    PeerNetworkRelationshipType RelationshipType,
    RelationshipStrength Strength,
    string Caption,
    DateOnly RelationshipStartDate,
    DateOnly? RelationshipEndDate = null,
    Guid? RelatedEntityId = null,
    Guid? ClassificationEntityId = null,
    decimal RolePairMultiplier = 1.0m,
    bool IsGrowthEnabled = true
) : ICommand<PeerNetworkEdgeResponse>;
