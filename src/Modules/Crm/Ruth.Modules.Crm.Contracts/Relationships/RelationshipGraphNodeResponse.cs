namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Represents a node returned from Apache AGE multi-hop CRM relationship graph traversal.</summary>
public sealed record RelationshipGraphNodeResponse(
    Guid EntityId,
    string Label,
    string DisplayName,
    int HopDistance
);
