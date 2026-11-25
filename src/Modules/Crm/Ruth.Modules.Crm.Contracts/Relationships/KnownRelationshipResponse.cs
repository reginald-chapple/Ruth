namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Represents a known relationship owned by a person.</summary>
public sealed record KnownRelationshipResponse(
    Guid Id,
    Guid OwnerPersonId,
    Guid RelatedPersonId,
    string RelatedPersonFullName,
    KnownRelationshipType RelationshipType,
    Guid? InverseRelationshipId,
    string? Notes,
    DateTimeOffset CreatedOnUtc
);
