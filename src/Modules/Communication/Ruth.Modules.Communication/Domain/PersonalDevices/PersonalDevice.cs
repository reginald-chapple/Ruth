using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.PersonalDevices;

/// <summary>Registered mobile/browser push notification device token linked optionally to a CRM person.</summary>
public sealed class PersonalDevice
    : AggregateRoot<PersonalDeviceId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.PersonalDevice";

    private PersonalDevice() { }

    private PersonalDevice(
        PersonalDeviceId id,
        Guid? personId,
        PersonalDevicePlatform platform,
        string pushRegistrationToken,
        string? deviceName,
        string? deviceVersion,
        bool notificationsEnabled,
        DateTimeOffset nowUtc
    )
        : base(id)
    {
        PersonId = personId == Guid.Empty ? null : personId;
        Platform = platform;
        PushRegistrationToken = pushRegistrationToken;
        DeviceName = deviceName;
        DeviceVersion = deviceVersion;
        NotificationsEnabled = notificationsEnabled;
        IsActive = true;
        LastSeenOnUtc = nowUtc.ToUniversalTime();
    }

    public Guid? PersonId { get; private set; }
    public PersonalDevicePlatform Platform { get; private set; }
    public string PushRegistrationToken { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? DeviceVersion { get; private set; }
    public bool NotificationsEnabled { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset LastSeenOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    /// <summary>Registers a new <see cref="PersonalDevice" /> push token.</summary>
    public static Result<PersonalDevice> Register(
        PersonalDevicePlatform platform,
        string pushRegistrationToken,
        Guid? personId,
        string? deviceName,
        string? deviceVersion,
        bool notificationsEnabled,
        DateTimeOffset nowUtc
    )
    {
        var tokenResult = ValidateToken(pushRegistrationToken);
        return tokenResult.IsFailure
            ? Result.Failure<PersonalDevice>(tokenResult.Errors)
            : new PersonalDevice(
                PersonalDeviceId.Create(),
                personId,
                platform,
                tokenResult.Value,
                CleanOptional(deviceName, 120),
                CleanOptional(deviceVersion, 64),
                notificationsEnabled,
                nowUtc
            );
    }

    /// <summary>Refreshes an existing device registration on repeated client sign-in or heartbeat.</summary>
    public void RefreshRegistration(
        PersonalDevicePlatform platform,
        Guid? personId,
        string? deviceName,
        string? deviceVersion,
        bool notificationsEnabled,
        DateTimeOffset nowUtc
    )
    {
        Platform = platform;
        if (personId.HasValue && personId.Value != Guid.Empty)
        {
            PersonId = personId;
        }

        DeviceName = CleanOptional(deviceName, 120) ?? DeviceName;
        DeviceVersion = CleanOptional(deviceVersion, 64) ?? DeviceVersion;
        NotificationsEnabled = notificationsEnabled;
        IsActive = true;
        LastSeenOnUtc = nowUtc.ToUniversalTime();
    }

    /// <summary>Updates opt-in and active state.</summary>
    public void SetNotificationPreferences(bool notificationsEnabled, bool isActive)
    {
        NotificationsEnabled = notificationsEnabled;
        IsActive = isActive;
    }

    /// <summary>Repoints <see cref="PersonId" /> when CRM people are merged.</summary>
    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    /// <summary>Validates and trims a push registration token.</summary>
    public static Result<string> ValidateToken(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return CommunicationDomainErrors.PersonalDeviceTokenInvalid;
        }

        var trimmed = rawToken.Trim();
        return trimmed.Length is < 8 or > 2_048 || trimmed.Any(char.IsWhiteSpace)
            ? CommunicationDomainErrors.PersonalDeviceTokenInvalid
            : trimmed;
    }

    private static string? CleanOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
