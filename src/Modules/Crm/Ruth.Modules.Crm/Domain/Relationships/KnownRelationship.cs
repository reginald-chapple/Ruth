using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Directed interpersonal relationship between two people paired with its reciprocal inverse record.</summary>
public sealed class KnownRelationship : AggregateRoot<KnownRelationshipId>, IAuditableEntity
{
    private KnownRelationship() { }

    private KnownRelationship(
        KnownRelationshipId id,
        PersonId ownerPersonId,
        PersonId relatedPersonId,
        KnownRelationshipType relationshipType,
        KnownRelationshipId? inverseRelationshipId,
        string? notes
    )
        : base(id)
    {
        OwnerPersonId = ownerPersonId;
        RelatedPersonId = relatedPersonId;
        RelationshipType = relationshipType;
        InverseRelationshipId = inverseRelationshipId;
        Notes = notes;
    }

    /// <summary>Gets the owning person identifier.</summary>
    public PersonId OwnerPersonId { get; private set; }

    /// <summary>Gets the related target person identifier.</summary>
    public PersonId RelatedPersonId { get; private set; }

    /// <summary>Gets the relationship classification.</summary>
    public KnownRelationshipType RelationshipType { get; private set; }

    /// <summary>Gets the linked reciprocal inverse relationship identifier.</summary>
    public KnownRelationshipId? InverseRelationshipId { get; private set; }

    /// <summary>Gets optional notes describing the relationship.</summary>
    public string? Notes { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a paired forward and reciprocal inverse relationship.</summary>
    public static Result<(KnownRelationship Forward, KnownRelationship Inverse)> CreatePair(
        PersonId ownerPersonId,
        PersonId relatedPersonId,
        KnownRelationshipType relationshipType,
        string? notes = null
    )
    {
        if (ownerPersonId == relatedPersonId)
        {
            return CrmErrors.RelationshipSelfReference;
        }

        var validatedNotes = CrmTextPolicy.ValidateOptionalText(notes, 500);
        if (validatedNotes.IsFailure)
        {
            return Result.Failure<(KnownRelationship, KnownRelationship)>(validatedNotes.Errors);
        }

        var forwardId = KnownRelationshipId.Create();
        var inverseId = KnownRelationshipId.Create();
        var inverseType = KnownRelationshipInverseMap.GetInverse(relationshipType);

        var forward = new KnownRelationship(
            forwardId,
            ownerPersonId,
            relatedPersonId,
            relationshipType,
            inverseId,
            validatedNotes.Value
        );
        var inverse = new KnownRelationship(
            inverseId,
            relatedPersonId,
            ownerPersonId,
            inverseType,
            forwardId,
            validatedNotes.Value
        );
        return Result.Success((forward, inverse));
    }

    internal void RepointOwner(PersonId newOwnerId)
    {
        OwnerPersonId = newOwnerId;
    }

    internal void RepointRelated(PersonId newRelatedId)
    {
        RelatedPersonId = newRelatedId;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
