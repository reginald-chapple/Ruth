using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

public sealed partial class Person
{
    /// <summary>Adds a new validated phone number to this person.</summary>
    public Result<PhoneNumber> AddPhoneNumber(PhoneNumberInputDto input)
    {
        var created = PhoneNumber.Create(Id, input);
        if (created.IsFailure)
        {
            return created;
        }

        _phoneNumbers.Add(created.Value);
        return created;
    }

    /// <summary>Updates an existing phone number owned by this person.</summary>
    public Result UpdatePhoneNumber(PhoneNumberId phoneNumberId, PhoneNumberInputDto input)
    {
        var existing = _phoneNumbers.Find(p => p.Id == phoneNumberId);
        return existing is null ? CrmErrors.PhoneNumberNotFound : existing.Update(input);
    }

    /// <summary>Removes a phone number owned by this person.</summary>
    public Result RemovePhoneNumber(PhoneNumberId phoneNumberId)
    {
        var index = _phoneNumbers.FindIndex(p => p.Id == phoneNumberId);
        if (index < 0)
        {
            return CrmErrors.PhoneNumberNotFound;
        }

        _phoneNumbers.RemoveAt(index);
        return Result.Success();
    }

    /// <summary>Records a previous last name unless an identical name already exists.</summary>
    public Result<PersonPreviousName> AddPreviousName(string lastName)
    {
        var created = PersonPreviousName.Create(Id, lastName);
        if (created.IsFailure)
        {
            return created;
        }

        var existing = _previousNames.Find(p =>
            string.Equals(
                p.NormalizedLastName,
                created.Value.NormalizedLastName,
                StringComparison.Ordinal
            )
        );
        if (existing is not null)
        {
            return Result.Success(existing);
        }

        _previousNames.Add(created.Value);
        return created;
    }

    /// <summary>Adds a search key, rejecting duplicates of the same type and value.</summary>
    public Result<PersonSearchKey> AddSearchKey(
        PersonSearchKeyType keyType,
        string searchValue,
        bool isValuePrivate = false,
        Guid? searchTypeValueId = null
    )
    {
        var created = PersonSearchKey.Create(
            Id,
            keyType,
            searchValue,
            isValuePrivate,
            searchTypeValueId
        );
        if (created.IsFailure)
        {
            return created;
        }

        if (
            _searchKeys.Exists(k =>
                k.KeyType == keyType
                && string.Equals(
                    k.NormalizedSearchValue,
                    created.Value.NormalizedSearchValue,
                    StringComparison.Ordinal
                )
            )
        )
        {
            return CrmErrors.SearchKeyDuplicate;
        }

        _searchKeys.Add(created.Value);
        return created;
    }

    /// <summary>Removes an existing search key from this person.</summary>
    public Result RemoveSearchKey(PersonSearchKeyId searchKeyId)
    {
        var index = _searchKeys.FindIndex(k => k.Id == searchKeyId);
        if (index < 0)
        {
            return CrmErrors.SearchKeyNotFound;
        }

        _searchKeys.RemoveAt(index);
        return Result.Success();
    }

    /// <summary>Updates primary household, campus, and age classification derived from household membership.</summary>
    public void SyncHouseholdDerivedState(
        Guid? primaryHouseholdId,
        Guid? primaryCampusId,
        Guid? givingHouseholdId,
        bool hasAdultRole,
        bool hasChildRole,
        DateOnly today
    )
    {
        PrimaryHouseholdId = primaryHouseholdId;
        if (primaryCampusId.HasValue)
        {
            PrimaryCampusId = primaryCampusId;
        }

        GivingHouseholdId = givingHouseholdId;
        var age = PersonDemographicsCalculator.CalculateAge(
            BirthYear,
            BirthMonth,
            BirthDay,
            today,
            DeceasedDate
        );
        AgeClassification = PersonDemographicsCalculator.ResolveAgeClassification(
            age,
            IsLockedAsChild,
            hasAdultRole,
            hasChildRole,
            AgeClassification
        );
    }

    /// <summary>Updates denormalized top active signal fields.</summary>
    public void SetTopSignal(Guid? signalId, string? color, string? icon)
    {
        TopSignalId = signalId;
        TopSignalColor = color;
        TopSignalIcon = icon;
    }

    /// <summary>Increments the denormalized profile view counter.</summary>
    public void IncrementViewedCount(int delta = 1)
    {
        ViewedCount = Math.Max(0, ViewedCount + delta);
    }

    /// <summary>Marks this person record as merged and retired into <paramref name="survivingPersonId" />.</summary>
    public void MarkMergedInto(PersonId survivingPersonId, DateTimeOffset mergedOnUtc)
    {
        IsMerged = true;
        MergedIntoPersonId = survivingPersonId.Value;
        RecordStatus = PersonRecordStatus.Inactive;
        RecordStatusLastModifiedOnUtc = mergedOnUtc.ToUniversalTime();
    }

    /// <summary>Applies an explicit earliest creation timestamp during person merge.</summary>
    public void PreserveEarliestCreatedOnUtc(DateTimeOffset candidateUtc)
    {
        var utc = candidateUtc.ToUniversalTime();
        if (CreatedOnUtc == default || utc < CreatedOnUtc)
        {
            CreatedOnUtc = utc;
        }
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

    /// <summary>Replaces all stored JSONB attribute values cleanly for EF Core change tracking.</summary>
    public void ReplaceAttributes(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var next = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                next[pair.Key.Trim()] = pair.Value;
            }
        }

        _attributes = next;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        if (CreatedOnUtc == default)
        {
            CreatedOnUtc = createdOnUtc.ToUniversalTime();
        }
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

    private void ArchiveChangedIdentityFields(string newLastName, string? newEmail)
    {
        if (
            !string.IsNullOrWhiteSpace(LastName)
            && !string.Equals(LastName, newLastName, StringComparison.OrdinalIgnoreCase)
        )
        {
            _ = AddPreviousName(LastName);
        }

        if (
            !string.IsNullOrWhiteSpace(Email)
            && !string.Equals(Email, newEmail, StringComparison.OrdinalIgnoreCase)
        )
        {
            _ = AddSearchKey(PersonSearchKeyType.Email, Email);
        }
    }

    private void ApplyValidated(
        PersonInput input,
        PersonValidationHelper.ValidatedPersonFields fields,
        DateTimeOffset nowUtc,
        bool isCreation
    )
    {
        var effectiveStatus = fields.IsDeceased ? PersonRecordStatus.Inactive : input.RecordStatus;
        if (isCreation || RecordStatus != effectiveStatus)
        {
            RecordStatus = effectiveStatus;
            RecordStatusLastModifiedOnUtc = nowUtc.ToUniversalTime();
        }

        RecordStatusReasonValueId = input.RecordStatusReasonValueId;
        ConnectionStatus = input.ConnectionStatus;
        ConnectionStatusValueId = input.ConnectionStatusValueId;
        AccountProtectionProfile = input.AccountProtectionProfile;
        Title = fields.Title;
        FirstName = fields.FirstName;
        NormalizedFirstName = fields.FirstName.ToUpperInvariant();
        NickName = fields.NickName;
        NormalizedNickName = fields.NickName.ToUpperInvariant();
        MiddleName = fields.MiddleName;
        LastName = fields.LastName;
        NormalizedLastName = fields.LastName.ToUpperInvariant();
        Suffix = fields.Suffix;
        Gender = input.Gender;
        MaritalStatus = input.MaritalStatus;
        AnniversaryDate = fields.AnniversaryDate;
        BirthYear = input.BirthYear;
        BirthMonth = input.BirthMonth;
        BirthDay = input.BirthDay;
        BirthDate = fields.BirthDate;
        GraduationYear = input.GraduationYear;
        IsDeceased = fields.IsDeceased;
        DeceasedDate = fields.DeceasedDate;
        IsLockedAsChild = input.IsLockedAsChild;
        Email = fields.Email;
        NormalizedEmail = fields.Email?.ToUpperInvariant();
        IsEmailActive = input.IsEmailActive;
        EmailNote = fields.EmailNote;
        EmailPreference = input.EmailPreference;
        CommunicationPreference = input.CommunicationPreference;
        PhotoBinaryFileId = input.PhotoBinaryFileId;
        PrimaryCampusId = input.PrimaryCampusId;

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var age = PersonDemographicsCalculator.CalculateAge(
            BirthYear,
            BirthMonth,
            BirthDay,
            today,
            DeceasedDate
        );
        AgeClassification = PersonDemographicsCalculator.ResolveAgeClassification(
            age,
            IsLockedAsChild,
            hasAdultHouseholdRole: false,
            hasChildHouseholdRole: false,
            AgeClassification
        );
    }
}
