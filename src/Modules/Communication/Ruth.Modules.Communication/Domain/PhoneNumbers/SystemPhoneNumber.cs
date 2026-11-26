using System.Text;
using System.Text.RegularExpressions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.PhoneNumbers;

/// <summary>Organization-owned E.164 phone number aggregate used for outbound SMS and inbound forwarding.</summary>
public sealed partial class SystemPhoneNumber
    : AggregateRoot<SystemPhoneNumberId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.SystemPhoneNumber";

    private SystemPhoneNumber() { }

    private SystemPhoneNumber(SystemPhoneNumberId id, Guid systemGuid)
        : base(id)
    {
        SystemGuid = systemGuid;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSmsEnabled { get; private set; } = true;
    public bool IsSmsForwardingEnabled { get; private set; }
    public Guid? AssignedToPersonId { get; private set; }
    public Guid? MobileApplicationSiteId { get; private set; }
    public string? ProviderIdentifier { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="SystemPhoneNumber" /> aggregate.</summary>
    public static Result<SystemPhoneNumber> Create(
        string name,
        string number,
        string? description = null,
        bool isSmsEnabled = true,
        bool isSmsForwardingEnabled = false,
        Guid? assignedToPersonId = null,
        Guid? mobileApplicationSiteId = null,
        string? providerIdentifier = null,
        bool isActive = true,
        Guid? systemGuid = null
    )
    {
        var id = SystemPhoneNumberId.Create();
        var entity = new SystemPhoneNumber(id, systemGuid ?? id.Value);
        var updated = entity.Update(
            name,
            number,
            description,
            isSmsEnabled,
            isSmsForwardingEnabled,
            assignedToPersonId,
            mobileApplicationSiteId,
            providerIdentifier,
            isActive
        );
        return updated.IsFailure ? Result.Failure<SystemPhoneNumber>(updated.Errors) : entity;
    }

    /// <summary>Updates properties on this <see cref="SystemPhoneNumber" />.</summary>
    public Result Update(
        string name,
        string number,
        string? description,
        bool isSmsEnabled,
        bool isSmsForwardingEnabled,
        Guid? assignedToPersonId,
        Guid? mobileApplicationSiteId,
        string? providerIdentifier,
        bool isActive
    )
    {
        if (!IsValidName(name))
        {
            return CommunicationDomainErrors.SystemPhoneNameInvalid;
        }

        var e164 = NormalizeE164(number);
        if (e164.IsFailure)
        {
            return Result.Failure(e164.Errors);
        }

        Name = name.Trim();
        Number = e164.Value;
        Description = CleanOptional(description);
        IsSmsEnabled = isSmsEnabled;
        IsSmsForwardingEnabled = isSmsForwardingEnabled;
        AssignedToPersonId = NormalizeOptionalGuid(assignedToPersonId);
        MobileApplicationSiteId = NormalizeOptionalGuid(mobileApplicationSiteId);
        ProviderIdentifier = CleanOptional(providerIdentifier);
        IsActive = isActive;
        return Result.Success();
    }

    /// <summary>Repoints <see cref="AssignedToPersonId" /> when CRM people are merged.</summary>
    public void RepointAssignedPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (AssignedToPersonId == retiredPersonId)
        {
            AssignedToPersonId = survivingPersonId;
        }
    }

    /// <summary>Normalizes a phone string into strict E.164 format (+[1-9][0-9]{6,14}).</summary>
    public static Result<string> NormalizeE164(string? rawNumber)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
        {
            return CommunicationDomainErrors.SystemPhoneNumberInvalid;
        }

        var stripped = ExtractDialCharacters(rawNumber.Trim());
        var candidate = EnsureLeadingCountryPlus(stripped);
        return E164Pattern.IsMatch(candidate)
            ? candidate
            : CommunicationDomainErrors.SystemPhoneNumberInvalid;
    }

    private static bool IsValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120;
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Guid? NormalizeOptionalGuid(Guid? id)
    {
        return id.HasValue && id.Value != Guid.Empty ? id : null;
    }

    private static string ExtractDialCharacters(string trimmed)
    {
        var sb = new StringBuilder(trimmed.Length);
        if (trimmed.StartsWith('+'))
        {
            _ = sb.Append('+');
        }

        foreach (var ch in trimmed)
        {
            if (char.IsAsciiDigit(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string EnsureLeadingCountryPlus(string stripped)
    {
        return stripped.StartsWith('+') ? stripped
            : stripped.Length == 10 ? $"+1{stripped}"
            : $"+{stripped}";
    }

    [GeneratedRegex(
        @"^\+[1-9][0-9]{6,14}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex E164Pattern { get; }
}
