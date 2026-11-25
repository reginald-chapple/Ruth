using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Immutable-key redirect ledger mapping retired PersonIds and legacy Rock PersonAlias GUIDs to surviving PersonIds.</summary>
public sealed class PersonRedirect : AggregateRoot<PersonRedirectId>, IAuditableEntity
{
    private PersonRedirect() { }

    private PersonRedirect(
        PersonRedirectId id,
        PersonId sourcePersonId,
        PersonId targetPersonId,
        Guid mergeBatchId,
        Guid? legacyPersonAliasGuid,
        Guid? mergedByUserId,
        DateTimeOffset mergedOnUtc
    )
        : base(id)
    {
        SourcePersonId = sourcePersonId;
        TargetPersonId = targetPersonId;
        MergeBatchId = mergeBatchId;
        LegacyPersonAliasGuid = legacyPersonAliasGuid;
        MergedByUserId = mergedByUserId;
        MergedOnUtc = mergedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the retired source person identifier.</summary>
    public PersonId SourcePersonId { get; private set; }

    /// <summary>Gets the canonical surviving person identifier.</summary>
    public PersonId TargetPersonId { get; private set; }

    /// <summary>Gets the merge batch correlation identifier.</summary>
    public Guid MergeBatchId { get; private set; }

    /// <summary>Gets an optional legacy Rock RMS PersonAlias GUID mapped to the survivor.</summary>
    public Guid? LegacyPersonAliasGuid { get; private set; }

    /// <summary>Gets the optional user identifier who executed the merge.</summary>
    public Guid? MergedByUserId { get; private set; }

    /// <summary>Gets the UTC timestamp when the merge occurred.</summary>
    public DateTimeOffset MergedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a new redirect ledger entry.</summary>
    public static PersonRedirect Create(
        PersonId sourcePersonId,
        PersonId targetPersonId,
        Guid mergeBatchId,
        DateTimeOffset mergedOnUtc,
        Guid? legacyPersonAliasGuid = null,
        Guid? mergedByUserId = null
    )
    {
        return new PersonRedirect(
            PersonRedirectId.Create(),
            sourcePersonId,
            targetPersonId,
            mergeBatchId,
            legacyPersonAliasGuid,
            mergedByUserId,
            mergedOnUtc
        );
    }

    /// <summary>Updates the target pointer during chained merges (A -> B -> C becomes A -> C).</summary>
    public void RepointTarget(PersonId newTargetPersonId)
    {
        TargetPersonId = newTargetPersonId;
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
