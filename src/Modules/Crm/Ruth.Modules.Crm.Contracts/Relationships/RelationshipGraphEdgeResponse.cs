namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Represents a directed edge returned from Apache AGE multi-hop CRM relationship graph traversal.</summary>
public sealed record RelationshipGraphEdgeResponse(
    Guid SourceId,
    Guid TargetId,
    string EdgeType,
    string Detail
);
