namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Multi-hop relationship graph centered on a person, backed by relational PostgreSQL CRM tables.</summary>
public sealed record PersonRelationshipGraphResponse(
    Guid RootPersonId,
    int MaxHops,
    IReadOnlyList<RelationshipGraphNodeResponse> Nodes,
    IReadOnlyList<RelationshipGraphEdgeResponse> Edges
);
