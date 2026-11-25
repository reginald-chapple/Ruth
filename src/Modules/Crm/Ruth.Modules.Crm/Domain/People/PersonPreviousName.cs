using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Tracks a prior last name for a <see cref="Person" /> to preserve searchability after name changes or merges.</summary>
public sealed class PersonPreviousName : Entity<PersonPreviousNameId>, IAuditableEntity
{
    private PersonPreviousName() { }

    private PersonPreviousName(PersonPreviousNameId id, PersonId personId, string lastName)
        : base(id)
    {
        PersonId = personId;
        LastName = lastName;
        NormalizedLastName = lastName.ToUpperInvariant();
    }

    /// <summary>Gets the owning person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the recorded previous last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized previous last name.</summary>
    public string NormalizedLastName { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a validated previous last name record.</summary>
    public static Result<PersonPreviousName> Create(PersonId personId, string lastName)
    {
        var sanitized = CrmTextPolicy.SanitizePersonName(lastName);
        return string.IsNullOrWhiteSpace(sanitized)
                ? (Result<PersonPreviousName>)CrmErrors.PersonLastNameRequired
            : sanitized.Length > CrmTextPolicy.NamePartMaxLength
                ? (Result<PersonPreviousName>)CrmErrors.PersonNameTooLong
            : Result.Success(
                new PersonPreviousName(PersonPreviousNameId.Create(), personId, sanitized)
            );
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
