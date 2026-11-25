namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Represents a scored directed peer-network connection between two people.</summary>
public sealed record PeerNetworkEdgeResponse(
    Guid Id,
    Guid SourcePersonId,
    Guid TargetPersonId,
    string TargetPersonFullName,
    PeerNetworkRelationshipType RelationshipType,
    Guid? RelatedEntityId,
    RelationshipStrength Strength,
    decimal RelationshipScore,
    RelationshipTrend Trend,
    string Caption,
    DateOnly RelationshipStartDate,
    DateOnly? RelationshipEndDate,
    DateTimeOffset LastUpdatedOnUtc
);
