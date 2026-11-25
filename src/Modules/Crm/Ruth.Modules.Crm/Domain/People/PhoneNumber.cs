using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Phone number entity owned by a <see cref="Person" /> with E.164 normalization and SMS opt-in/out invariants.</summary>
public sealed class PhoneNumber : Entity<PhoneNumberId>, IAuditableEntity
{
    private PhoneNumber() { }

    private PhoneNumber(
        PhoneNumberId id,
        PersonId personId,
        PhoneNumberType numberType,
        Guid? numberTypeValueId,
        NormalizedPhoneNumber normalized,
        string? extension,
        bool isMessagingEnabled,
        bool isUnlisted,
        string? description
    )
        : base(id)
    {
        PersonId = personId;
        NumberType = numberType;
        NumberTypeValueId = numberTypeValueId;
        CountryCode = normalized.CountryCode;
        Number = normalized.Digits;
        E164Number = normalized.E164Number;
        NumberFormatted = normalized.NumberFormatted;
        NumberReversed = normalized.NumberReversed;
        Extension = extension;
        IsMessagingEnabled = isMessagingEnabled;
        IsUnlisted = isUnlisted;
        Description = description;
    }

    /// <summary>Gets the owning person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the phone number category.</summary>
    public PhoneNumberType NumberType { get; private set; }

    /// <summary>Gets the optional Core DefinedValue identifier for custom phone types.</summary>
    public Guid? NumberTypeValueId { get; private set; }

    /// <summary>Gets the calling country code (e.g. "1").</summary>
    public string CountryCode { get; private set; } = PhoneNumberNormalizer.DefaultCountryCode;

    /// <summary>Gets the clean digits-only phone number.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>Gets the E.164 formatted number (+CountryCodeDigits).</summary>
    public string E164Number { get; private set; } = string.Empty;

    /// <summary>Gets the display-formatted phone number.</summary>
    public string NumberFormatted { get; private set; } = string.Empty;

    /// <summary>Gets the reversed digits for fast suffix lookups.</summary>
    public string NumberReversed { get; private set; } = string.Empty;

    /// <summary>Gets the optional extension.</summary>
    public string? Extension { get; private set; }

    /// <summary>Gets whether SMS messaging is enabled for this number.</summary>
    public bool IsMessagingEnabled { get; private set; }

    /// <summary>Gets whether the recipient has opted out of SMS messages on this number.</summary>
    public bool IsMessagingOptedOut { get; private set; }

    /// <summary>Gets the UTC timestamp when the recipient opted out of SMS.</summary>
    public DateTimeOffset? MessagingOptedOutOnUtc { get; private set; }

    /// <summary>Gets whether the number is unlisted.</summary>
    public bool IsUnlisted { get; private set; }

    /// <summary>Gets the optional note or description.</summary>
    public string? Description { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a validated, normalized phone number.</summary>
    public static Result<PhoneNumber> Create(PersonId personId, PhoneNumberInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var normalized = PhoneNumberNormalizer.Normalize(input.Number, input.CountryCode);
        if (normalized.IsFailure)
        {
            return Result.Failure<PhoneNumber>(normalized.Errors);
        }

        var ext = CrmTextPolicy.ValidateOptionalText(input.Extension, 20, allowNewLines: false);
        var desc = CrmTextPolicy.ValidateOptionalText(input.Description, 200, allowNewLines: false);
        var combined = Result.Combine(ext, desc);
        return combined.IsFailure
            ? Result.Failure<PhoneNumber>(combined.Errors)
            : Result.Success(
                new PhoneNumber(
                    PhoneNumberId.Create(),
                    personId,
                    input.NumberType,
                    input.NumberTypeValueId,
                    normalized.Value,
                    ext.Value,
                    input.IsMessagingEnabled ?? false,
                    input.IsUnlisted,
                    desc.Value
                )
            );
    }

    /// <summary>Updates the phone number enforcing Rock RMS SMS consent and opt-out reset invariants.</summary>
    public Result Update(PhoneNumberInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var normalized = PhoneNumberNormalizer.Normalize(input.Number, input.CountryCode);
        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Errors);
        }

        var ext = CrmTextPolicy.ValidateOptionalText(input.Extension, 20, allowNewLines: false);
        var desc = CrmTextPolicy.ValidateOptionalText(input.Description, 200, allowNewLines: false);
        var combined = Result.Combine(ext, desc);
        if (combined.IsFailure)
        {
            return combined;
        }

        var digitsChanged = !string.Equals(
            Number,
            normalized.Value.Digits,
            StringComparison.Ordinal
        );
        var fullNumberChanged =
            digitsChanged
            || !string.Equals(CountryCode, normalized.Value.CountryCode, StringComparison.Ordinal);

        NumberType = input.NumberType;
        NumberTypeValueId = input.NumberTypeValueId;
        CountryCode = normalized.Value.CountryCode;
        Number = normalized.Value.Digits;
        E164Number = normalized.Value.E164Number;
        NumberFormatted = normalized.Value.NumberFormatted;
        NumberReversed = normalized.Value.NumberReversed;
        Extension = ext.Value;
        IsUnlisted = input.IsUnlisted;
        Description = desc.Value;

        if (input.IsMessagingEnabled.HasValue)
        {
            IsMessagingEnabled = input.IsMessagingEnabled.Value;
        }
        else if (fullNumberChanged)
        {
            IsMessagingEnabled = false;
        }

        if (digitsChanged && IsMessagingOptedOut)
        {
            IsMessagingOptedOut = false;
            MessagingOptedOutOnUtc = null;
        }

        return Result.Success();
    }

    /// <summary>Records an SMS opt-out event on this phone number.</summary>
    public void RecordMessagingOptOut(DateTimeOffset optedOutOnUtc)
    {
        IsMessagingOptedOut = true;
        MessagingOptedOutOnUtc = optedOutOnUtc.ToUniversalTime();
    }

    /// <summary>Enables SMS consent (used during person merge consent union).</summary>
    public void EnableMessagingConsent()
    {
        IsMessagingEnabled = true;
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
    public override string ToString()
    {
        return IsUnlisted ? "Unlisted" : NumberFormatted;
    }
}
