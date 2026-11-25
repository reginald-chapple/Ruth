using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Alternate lookup key (alternate ID, archived email, barcode, legacy ID) owned by a <see cref="Person" />.</summary>
public sealed class PersonSearchKey : Entity<PersonSearchKeyId>, IAuditableEntity
{
    private PersonSearchKey() { }

    private PersonSearchKey(
        PersonSearchKeyId id,
        PersonId personId,
        PersonSearchKeyType keyType,
        Guid? searchTypeValueId,
        string searchValue,
        bool isValuePrivate
    )
        : base(id)
    {
        PersonId = personId;
        KeyType = keyType;
        SearchTypeValueId = searchTypeValueId;
        SearchValue = searchValue;
        NormalizedSearchValue = searchValue.ToUpperInvariant();
        IsValuePrivate = isValuePrivate;
    }

    /// <summary>Gets the owning person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the search key category.</summary>
    public PersonSearchKeyType KeyType { get; private set; }

    /// <summary>Gets the optional Core DefinedValue identifier for custom search key types.</summary>
    public Guid? SearchTypeValueId { get; private set; }

    /// <summary>Gets the search key value.</summary>
    public string SearchValue { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized search key value.</summary>
    public string NormalizedSearchValue { get; private set; } = string.Empty;

    /// <summary>Gets whether this value is hidden in standard staff UI views.</summary>
    public bool IsValuePrivate { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a validated search key.</summary>
    public static Result<PersonSearchKey> Create(
        PersonId personId,
        PersonSearchKeyType keyType,
        string searchValue,
        bool isValuePrivate = false,
        Guid? searchTypeValueId = null
    )
    {
        var validated = CrmTextPolicy.ValidateRequiredText(
            searchValue,
            CrmTextPolicy.SearchValueMaxLength,
            CrmErrors.SearchKeyRequired,
            CrmErrors.SearchKeyTooLong
        );
        return validated.IsFailure
            ? Result.Failure<PersonSearchKey>(validated.Errors)
            : Result.Success(
                new PersonSearchKey(
                    PersonSearchKeyId.Create(),
                    personId,
                    keyType,
                    searchTypeValueId,
                    validated.Value,
                    isValuePrivate
                )
            );
    }

    /// <summary>Generates a Rock-style 14-hex-character AlternateId formatted as xxxxxxx-xxxxxxx.</summary>
    public static string GenerateAlternateId()
    {
        var hex = Guid.CreateVersion7().ToString("N");
        return $"{hex[..7]}-{hex[7..14]}";
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
