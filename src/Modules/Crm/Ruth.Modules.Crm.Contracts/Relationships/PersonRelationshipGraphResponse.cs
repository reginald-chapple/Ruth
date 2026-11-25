namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Multi-hop relationship graph centered on a person, backed by PostgreSQL Apache AGE (crm_graph).</summary>
public sealed record PersonRelationshipGraphResponse(
    Guid RootPersonId,
    int MaxHops,
    bool PoweredByApacheAge,
    IReadOnlyList<RelationshipGraphNodeResponse> Nodes,
    IReadOnlyList<RelationshipGraphEdgeResponse> Edges
);
