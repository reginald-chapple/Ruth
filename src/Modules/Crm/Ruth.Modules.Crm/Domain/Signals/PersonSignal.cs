using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Signals;

/// <summary>Safety/VIP signal attached to a <see cref="Person" /> inheriting ACL authority from its <see cref="SignalType" />.</summary>
public sealed class PersonSignal : AggregateRoot<PersonSignalId>, IAuditableEntity, ISecured
{
    /// <summary>Canonical entity type key for PersonSignal.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.PersonSignal";

    private PersonSignal() { }

    private PersonSignal(
        PersonSignalId id,
        PersonId personId,
        SignalTypeId signalTypeId,
        PersonId ownerPersonId,
        string? note,
        DateTimeOffset? expirationDateUtc
    )
        : base(id)
    {
        PersonId = personId;
        SignalTypeId = signalTypeId;
        OwnerPersonId = ownerPersonId;
        Note = note;
        ExpirationDateUtc = expirationDateUtc?.ToUniversalTime();
    }

    /// <summary>Gets the target person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the signal type identifier.</summary>
    public SignalTypeId SignalTypeId { get; private set; }

    /// <summary>Gets the person who created/owns this signal report.</summary>
    public PersonId OwnerPersonId { get; private set; }

    /// <summary>Gets optional context notes.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the optional UTC expiration timestamp.</summary>
    public DateTimeOffset? ExpirationDateUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            SignalType.EntityTypeKeyName,
            SignalTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Returns whether the signal is currently active at <paramref name="nowUtc" />.</summary>
    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return !ExpirationDateUtc.HasValue || ExpirationDateUtc.Value >= nowUtc.ToUniversalTime();
    }

    /// <summary>Creates a validated person signal.</summary>
    public static Result<PersonSignal> Create(
        PersonId personId,
        SignalTypeId signalTypeId,
        PersonId ownerPersonId,
        string? note = null,
        DateTimeOffset? expirationDateUtc = null
    )
    {
        var validatedNote = CrmTextPolicy.ValidateOptionalText(
            note,
            CrmTextPolicy.DescriptionMaxLength
        );
        return validatedNote.IsFailure
            ? Result.Failure<PersonSignal>(validatedNote.Errors)
            : Result.Success(
                new PersonSignal(
                    PersonSignalId.Create(),
                    personId,
                    signalTypeId,
                    ownerPersonId,
                    validatedNote.Value,
                    expirationDateUtc
                )
            );
    }

    internal void RepointPerson(PersonId targetPersonId)
    {
        PersonId = targetPersonId;
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
