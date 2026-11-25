using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>First-class Household (family or shared billing/member unit) aggregate.</summary>
public sealed class Household
    : AggregateRoot<HouseholdId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for Household.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.Household";

    private readonly List<HouseholdMember> _members = [];
    private readonly List<HouseholdLocation> _locations = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private Household() { }

    private Household(
        HouseholdId id,
        string name,
        Guid? branchId,
        bool combineContributionsByDefault
    )
        : base(id)
    {
        Name = name;
        BranchId = branchId;
        CombineContributionsByDefault = combineContributionsByDefault;
        IsActive = true;
    }

    /// <summary>Gets the household display name (e.g. "Smith Household").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional Core Branch identifier.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets whether the household is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether the household has been archived.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets whether new members default to combined contributions/billing in this household.</summary>
    public bool CombineContributionsByDefault { get; private set; } = true;

    /// <summary>Gets the household memberships.</summary>
    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    /// <summary>Gets the physical locations associated with this household.</summary>
    public IReadOnlyCollection<HouseholdLocation> Locations => _locations.AsReadOnly();

    /// <summary>Gets the JSONB attribute map persisted by EF Core.</summary>
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

    /// <summary>Creates a validated household aggregate.</summary>
    public static Result<Household> Create(
        string name,
        Guid? branchId = null,
        bool combineContributionsByDefault = true
    )
    {
        var validated = CrmTextPolicy.ValidateRequiredText(
            name,
            CrmTextPolicy.HouseholdNameMaxLength,
            CrmErrors.HouseholdNameRequired,
            CrmErrors.HouseholdNameTooLong
        );
        return validated.IsFailure
            ? Result.Failure<Household>(validated.Errors)
            : Result.Success(
                new Household(
                    HouseholdId.Create(),
                    validated.Value,
                    branchId,
                    combineContributionsByDefault
                )
            );
    }

    /// <summary>Updates household metadata.</summary>
    public Result Update(
        string name,
        Guid? branchId,
        bool isActive,
        bool combineContributionsByDefault
    )
    {
        var validated = CrmTextPolicy.ValidateRequiredText(
            name,
            CrmTextPolicy.HouseholdNameMaxLength,
            CrmErrors.HouseholdNameRequired,
            CrmErrors.HouseholdNameTooLong
        );
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Errors);
        }

        Name = validated.Value;
        BranchId = branchId;
        IsActive = isActive;
        CombineContributionsByDefault = combineContributionsByDefault;
        return Result.Success();
    }

    /// <summary>Adds or updates a person's membership inside this household.</summary>
    public Result<HouseholdMember> AddOrUpdateMember(
        PersonId personId,
        HouseholdRole role,
        int householdOrder = 0,
        bool? isCombinedContributionMember = null,
        bool? isPrimaryContact = null
    )
    {
        var effectiveCombined = isCombinedContributionMember ?? CombineContributionsByDefault;
        var existing = _members.Find(m => m.PersonId == personId);
        var hasOtherPrimary = _members.Exists(m => m.PersonId != personId && m.IsPrimaryContact);
        var effectivePrimary = isPrimaryContact
            ?? existing?.IsPrimaryContact
            ?? (!hasOtherPrimary && role == HouseholdRole.Adult);

        if (effectivePrimary)
        {
            foreach (var other in _members.Where(m => m.PersonId != personId && m.IsPrimaryContact))
            {
                other.ClearPrimaryContact();
            }
        }

        if (existing is not null)
        {
            existing.Update(role, householdOrder, effectiveCombined, effectivePrimary);
            return Result.Success(existing);
        }

        var member = new HouseholdMember(
            HouseholdMemberId.Create(),
            Id,
            personId,
            role,
            householdOrder,
            effectiveCombined,
            effectivePrimary
        );
        _members.Add(member);
        return Result.Success(member);
    }

    /// <summary>Removes a person from this household.</summary>
    public Result RemoveMember(PersonId personId)
    {
        var index = _members.FindIndex(m => m.PersonId == personId);
        if (index < 0)
        {
            return CrmErrors.HouseholdMemberNotFound;
        }

        _members.RemoveAt(index);
        return Result.Success();
    }

    /// <summary>Attaches a location, demoting prior same-type addresses to Previous when requested.</summary>
    public Result<HouseholdLocation> SetLocation(
        Guid locationId,
        HouseholdLocationType locationType,
        bool isMailingLocation,
        bool isMappedLocation,
        string? addressSummary,
        bool moveExistingToPrevious = true
    )
    {
        if (locationId == Guid.Empty)
        {
            return CrmErrors.HouseholdLocationInvalid;
        }

        var summary = CrmTextPolicy.ValidateOptionalText(addressSummary, 300, allowNewLines: false);
        if (summary.IsFailure)
        {
            return Result.Failure<HouseholdLocation>(summary.Errors);
        }

        if (moveExistingToPrevious && locationType != HouseholdLocationType.Previous)
        {
            foreach (
                var existing in _locations.Where(l =>
                    l.LocationType == locationType && l.LocationId != locationId
                )
            )
            {
                existing.DemoteToPrevious();
            }
        }

        var match = _locations.Find(l => l.LocationId == locationId);
        if (match is not null)
        {
            match.Update(locationType, isMailingLocation, isMappedLocation, summary.Value);
            return Result.Success(match);
        }

        var added = new HouseholdLocation(
            HouseholdLocationId.Create(),
            Id,
            locationId,
            locationType,
            isMailingLocation,
            isMappedLocation,
            summary.Value
        );
        _locations.Add(added);
        return Result.Success(added);
    }

    /// <summary>Archives this household (e.g. when emptied during person merge).</summary>
    public void Archive()
    {
        IsActive = false;
        IsArchived = true;
    }

    /// <inheritdoc />
    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributes = new Dictionary<string, string?>(_attributes, StringComparer.OrdinalIgnoreCase)
        {
            [key.Trim()] = value,
        };
    }

    /// <inheritdoc />
    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var next = new Dictionary<string, string?>(_attributes, StringComparer.OrdinalIgnoreCase);
        _ = next.Remove(key.Trim());
        _attributes = next;
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

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
