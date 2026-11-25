using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Core CRM Person aggregate owning demographics, contact info, search keys, previous names, and JSONB attributes.</summary>
public sealed partial class Person
    : AggregateRoot<PersonId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for Person.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.Person";

    private readonly List<PhoneNumber> _phoneNumbers = [];
    private readonly List<PersonPreviousName> _previousNames = [];
    private readonly List<PersonSearchKey> _searchKeys = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private Person() { }

    private Person(PersonId id, PersonRecordType recordType)
        : base(id)
    {
        RecordType = recordType;
    }

    /// <summary>Gets the record type classification.</summary>
    public PersonRecordType RecordType { get; private set; }

    /// <summary>Gets the record lifecycle status.</summary>
    public PersonRecordStatus RecordStatus { get; private set; }

    /// <summary>Gets the optional Core DefinedValue reason for record status.</summary>
    public Guid? RecordStatusReasonValueId { get; private set; }

    /// <summary>Gets the UTC timestamp when record status last changed.</summary>
    public DateTimeOffset? RecordStatusLastModifiedOnUtc { get; private set; }

    /// <summary>Gets the engagement/connection status.</summary>
    public PersonConnectionStatus ConnectionStatus { get; private set; }

    /// <summary>Gets the optional Core DefinedValue for connection status.</summary>
    public Guid? ConnectionStatusValueId { get; private set; }

    /// <summary>Gets the protection profile governing sensitive operations and merge authorization.</summary>
    public AccountProtectionProfile AccountProtectionProfile { get; private set; }

    /// <summary>Gets the optional honorific title.</summary>
    public string? Title { get; private set; }

    /// <summary>Gets the legal first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized first name for indexed phonetic/exact lookups.</summary>
    public string NormalizedFirstName { get; private set; } = string.Empty;

    /// <summary>Gets the preferred nickname (defaults to <see cref="FirstName" />).</summary>
    public string NickName { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized nickname for indexed phonetic/exact lookups.</summary>
    public string NormalizedNickName { get; private set; } = string.Empty;

    /// <summary>Gets the optional middle name.</summary>
    public string? MiddleName { get; private set; }

    /// <summary>Gets the last name (or business name when <see cref="RecordType" /> is Business).</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized last name for indexed phonetic/exact lookups.</summary>
    public string NormalizedLastName { get; private set; } = string.Empty;

    /// <summary>Gets the optional name suffix.</summary>
    public string? Suffix { get; private set; }

    /// <summary>Gets the gender classification.</summary>
    public Gender Gender { get; private set; }

    /// <summary>Gets the marital status.</summary>
    public MaritalStatus MaritalStatus { get; private set; }

    /// <summary>Gets the wedding anniversary date (null when not Married).</summary>
    public DateOnly? AnniversaryDate { get; private set; }

    /// <summary>Gets the optional birth year.</summary>
    public int? BirthYear { get; private set; }

    /// <summary>Gets the optional birth month (1–12).</summary>
    public int? BirthMonth { get; private set; }

    /// <summary>Gets the optional birth day (1–31).</summary>
    public int? BirthDay { get; private set; }

    /// <summary>Gets the computed birth date when month and day are present.</summary>
    public DateOnly? BirthDate { get; private set; }

    /// <summary>Gets the expected or past high-school graduation year.</summary>
    public int? GraduationYear { get; private set; }

    /// <summary>Gets whether the person is deceased.</summary>
    public bool IsDeceased { get; private set; }

    /// <summary>Gets the optional date of passing.</summary>
    public DateOnly? DeceasedDate { get; private set; }

    /// <summary>Gets whether this person is locked as a child regardless of age.</summary>
    public bool IsLockedAsChild { get; private set; }

    /// <summary>Gets the adult vs. child classification.</summary>
    public AgeClassification AgeClassification { get; private set; }

    /// <summary>Gets the primary email address.</summary>
    public string? Email { get; private set; }

    /// <summary>Gets the uppercase normalized email address.</summary>
    public string? NormalizedEmail { get; private set; }

    /// <summary>Gets whether the primary email is active.</summary>
    public bool IsEmailActive { get; private set; } = true;

    /// <summary>Gets optional notes about the email address (e.g. bounce reason).</summary>
    public string? EmailNote { get; private set; }

    /// <summary>Gets the email preference.</summary>
    public EmailPreference EmailPreference { get; private set; }

    /// <summary>Gets the preferred communication channel.</summary>
    public CommunicationPreference CommunicationPreference { get; private set; } =
        CommunicationPreference.Email;

    /// <summary>Gets the optional Core BinaryFile identifier for the profile photo.</summary>
    public Guid? PhotoBinaryFileId { get; private set; }

    /// <summary>Gets the optional primary Core Campus identifier.</summary>
    public Guid? PrimaryCampusId { get; private set; }

    /// <summary>Gets the optional primary household identifier.</summary>
    public Guid? PrimaryHouseholdId { get; private set; }

    /// <summary>Gets the optional household identifier used for combined giving.</summary>
    public Guid? GivingHouseholdId { get; private set; }

    /// <summary>Gets the highest-priority active signal identifier (denormalized).</summary>
    public Guid? TopSignalId { get; private set; }

    /// <summary>Gets the highest-priority active signal color (denormalized).</summary>
    public string? TopSignalColor { get; private set; }

    /// <summary>Gets the highest-priority active signal icon (denormalized).</summary>
    public string? TopSignalIcon { get; private set; }

    /// <summary>Gets the denormalized profile view count.</summary>
    public int ViewedCount { get; private set; }

    /// <summary>Gets whether this record has been retired via merge.</summary>
    public bool IsMerged { get; private set; }

    /// <summary>Gets the surviving person identifier when <see cref="IsMerged" /> is true.</summary>
    public Guid? MergedIntoPersonId { get; private set; }

    /// <summary>Gets the owned phone numbers.</summary>
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers => _phoneNumbers.AsReadOnly();

    /// <summary>Gets the recorded previous last names.</summary>
    public IReadOnlyCollection<PersonPreviousName> PreviousNames => _previousNames.AsReadOnly();

    /// <summary>Gets the alternate search keys.</summary>
    public IReadOnlyCollection<PersonSearchKey> SearchKeys => _searchKeys.AsReadOnly();

    /// <summary>Gets the JSONB attributes map persisted by EF Core.</summary>
    public IReadOnlyDictionary<string, string?> Attributes
    {
        get => _attributes;
        private set =>
            _attributes = value is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> AttributeValues => _attributes;

    /// <summary>Gets the standard display name.</summary>
    public string FullName =>
        PersonNameFormatter.FormatFullName(RecordType, NickName, LastName, Suffix);

    /// <summary>Gets the reversed display name.</summary>
    public string FullNameReversed =>
        PersonNameFormatter.FormatFullNameReversed(RecordType, NickName, LastName, Suffix);

    /// <summary>Gets the formal display name.</summary>
    public string FullNameFormal =>
        PersonNameFormatter.FormatFullNameFormal(RecordType, Title, FirstName, LastName, Suffix);

    /// <summary>Gets the 1–2 letter uppercase initials.</summary>
    public string Initials => PersonNameFormatter.FormatInitials(NickName, LastName);

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Determines whether this person may receive transactional or bulk email.</summary>
    public bool CanReceiveEmail(bool isBulk = true)
    {
        return !string.IsNullOrWhiteSpace(Email)
            && IsEmailActive
            && EmailPreference != EmailPreference.DoNotEmail
            && (!isBulk || EmailPreference != EmailPreference.NoMassEmails);
    }

    /// <summary>Creates a new validated <see cref="Person" /> aggregate with an auto-generated AlternateId search key.</summary>
    public static Result<Person> Create(PersonInput input, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validated = PersonValidationHelper.Validate(input);
        if (validated.IsFailure)
        {
            return Result.Failure<Person>(validated.Errors);
        }

        var person = new Person(PersonId.Create(), input.RecordType);
        person.ApplyValidated(input, validated.Value, nowUtc, isCreation: true);
        var altId = PersonSearchKey.Create(
            person.Id,
            PersonSearchKeyType.AlternateId,
            PersonSearchKey.GenerateAlternateId(),
            isValuePrivate: false
        );
        if (altId.IsSuccess)
        {
            person._searchKeys.Add(altId.Value);
        }

        return Result.Success(person);
    }

    /// <summary>Updates person profile fields, archiving previous LastName and Email automatically.</summary>
    public Result Update(PersonInput input, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (IsMerged)
        {
            return CrmErrors.PersonAlreadyMerged;
        }

        var validated = PersonValidationHelper.Validate(input);
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Errors);
        }

        ArchiveChangedIdentityFields(validated.Value.LastName, validated.Value.Email);
        ApplyValidated(input, validated.Value, nowUtc, isCreation: false);
        return Result.Success();
    }

    /// <summary>Distinct non-empty name tokens used to populate the Double Metaphone index.</summary>
    public IReadOnlyList<string> GetMetaphoneTokens()
    {
        return
        [
            .. new[] { FirstName, NickName, LastName }
                .Where(static n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }
}
