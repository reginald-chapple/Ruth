using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Command to create a known relationship and its reciprocal inverse relationship, syncing Apache AGE.</summary>
public sealed record AddKnownRelationshipCommand(
    Guid OwnerPersonId,
    Guid RelatedPersonId,
    KnownRelationshipType RelationshipType,
    string? Notes = null
) : ICommand<KnownRelationshipResponse>;
