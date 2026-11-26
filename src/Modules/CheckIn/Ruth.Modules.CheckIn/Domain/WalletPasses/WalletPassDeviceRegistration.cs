using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Apple PassKit device registration linking a deviceLibraryIdentifier and APNs pushToken to an issued pass.</summary>
public sealed class WalletPassDeviceRegistration
    : Entity<WalletPassDeviceRegistrationId>,
        IAuditableEntity
{
    private WalletPassDeviceRegistration() { }

    private WalletPassDeviceRegistration(
        WalletPassDeviceRegistrationId id,
        IssuedWalletPassId issuedPassId,
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string pushToken,
        DateTimeOffset registeredAtUtc
    )
        : base(id)
    {
        IssuedPassId = issuedPassId;
        DeviceLibraryIdentifier = deviceLibraryIdentifier;
        PassTypeIdentifier = passTypeIdentifier;
        SerialNumber = serialNumber;
        PushToken = pushToken;
        RegisteredAtUtc = registeredAtUtc;
    }

    public IssuedWalletPassId IssuedPassId { get; private set; }
    public string DeviceLibraryIdentifier { get; private set; } = string.Empty;
    public string PassTypeIdentifier { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string PushToken { get; private set; } = string.Empty;
    public DateTimeOffset RegisteredAtUtc { get; private set; }
    public DateTimeOffset? LastNotifiedAtUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <summary>Creates a validated <see cref="WalletPassDeviceRegistration" />.</summary>
    public static Result<WalletPassDeviceRegistration> Create(
        IssuedWalletPass pass,
        string deviceLibraryIdentifier,
        string pushToken,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(pass);
        return string.IsNullOrWhiteSpace(pushToken)
            ? Result.Failure<WalletPassDeviceRegistration>(WalletPassErrors.PushTokenRequired)
            : new WalletPassDeviceRegistration(
                WalletPassDeviceRegistrationId.Create(),
                pass.Id,
                deviceLibraryIdentifier.Trim(),
                pass.PassTypeIdentifier,
                pass.SerialNumber,
                pushToken.Trim(),
                nowUtc.ToUniversalTime()
            );
    }

    /// <summary>Refreshes the APNs push token for an existing device registration.</summary>
    public Result UpdatePushToken(string pushToken, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return WalletPassErrors.PushTokenRequired;
        }

        PushToken = pushToken.Trim();
        ModifiedOnUtc = nowUtc.ToUniversalTime();
        return Result.Success();
    }

    /// <summary>Records when a PassKit push notification was dispatched to this registration.</summary>
    public void MarkNotified(DateTimeOffset nowUtc)
    {
        LastNotifiedAtUtc = nowUtc.ToUniversalTime();
    }
}
